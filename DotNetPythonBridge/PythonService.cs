using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading;
using DotNetPythonBridge.Utils;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DotNetPythonBridge
{
    public class PythonService : IDisposable, IAsyncDisposable
    {
        private Process _process;
        public int Port { get; private set; }
        public int Pid => _process?.Id ?? -1;
        public WSL_Helper.WSL_Distro? _wsl = null;
        private static HttpClient client = new HttpClient(); // shared HttpClient for health checks


        private PythonService(Process proc, int port, WSL_Helper.WSL_Distro? wsl = null)
        {
            _process = proc;
            Port = port;
            _wsl = wsl;
        }

        /// <summary>
        /// Start a long-running Python service inside the given conda environment.
        /// </summary>
        public static async Task<PythonService> Start(string scriptPath, PythonEnvironment? env = null, PythonServiceOptions? options = null)
        {
            Log.Logger.LogInformation($"Starting Python service using environment: {(env != null ? env.Name : "Base")}, script: {scriptPath}");

            options ??= new PythonServiceOptions(); // use default options if none provided

            if (!File.Exists(scriptPath))
            {
                Log.Logger.LogError($"Script not found: {scriptPath}");
                throw new FileNotFoundException($"Script not found: {scriptPath}");
            }

            // Reserve port if requested
            int port = options.DefaultPort == 0 ? PortHelper.GetFreePort() : options.DefaultPort; // if 0, get free port

            // Resolve python executable inside the env
            string pythonExe = await PythonRunner.GetPythonExecutable(env);

            // Arguments: script + port + user args
            string args = $"\"{scriptPath}\" --port {port} {options.DefaultServiceArgs}".Trim();

            var proc = await ProcessHelper.StartProcess(pythonExe, args);

            var service = new PythonService(proc, port);

            Log.Logger.LogInformation($"Started Python service (PID: {service.Pid}) on port {service.Port} using script: {scriptPath}");

            // Optional health check
            if (options.HealthCheckEnabled && !await service.WaitForHealthCheck(options))
            {
                await service.Stop();
                throw new Exception("Python service failed to become healthy.");
            }
            Log.Logger.LogInformation($"Python service (PID: {service.Pid}) is healthy on port {service.Port}");


            return service;
        }

        /// <summary>
        /// Start a long-running Python service inside the given conda environment.
        /// </summary>
        public static async Task<PythonService> StartWSL(string scriptPath, PythonEnvironment? env = null, PythonServiceOptions? options = null, WSL_Helper.WSL_Distro? wsl = null)
        {
            Log.Logger.LogInformation($"Starting Python service using environment: {(env != null ? env.Name : "Base")}, script: {scriptPath}, WSL: {(wsl != null ? wsl.Name : "None")}");

            if (wsl == null)
            {
                wsl = await WSL_Helper.getDefaultWSL_Distro();
                Log.Logger.LogInformation($"No WSL distro specified. Using default: {wsl.Name}");
            }

            options ??= new PythonServiceOptions();

            if (!File.Exists(scriptPath)) //ensure the script path exists on Windows side
            {
                Log.Logger.LogError($"Script not found: {scriptPath}");
                throw new FileNotFoundException($"Script not found: {scriptPath}");
            }

            // Reserve port if requested
            int port = options.DefaultPort == 0 ? PortHelper.GetFreePort() : options.DefaultPort;

            // Resolve python executable inside the env
            string pythonExe = await PythonRunner.GetPythonExecutableWSL(env, wsl);

            // Prepend with wsl -d <distro> to run inside WSL and use bash -lic to ensure env is loaded correctly
            string args = $"bash -lic \"{pythonExe} \\\"{FilenameHelper.convertWindowsPathToWSL(scriptPath)}\\\" --port {port} {options.DefaultServiceArgs}\"".Trim();

            var proc = await ProcessHelper.StartProcess("wsl", $"-d {wsl.Name} {args}");

            var service = new PythonService(proc, port, wsl);

            Log.Logger.LogInformation($"Started Python service in WSL distro '{wsl.Name}' (PID: {service.Pid}) on port {service.Port} using script: {scriptPath}");

            // Optional health check
            if (options.HealthCheckEnabled && !await service.WaitForHealthCheck(options))
            {
                await service.Stop();
                throw new Exception("Python service failed to become healthy.");
            }
            Log.Logger.LogInformation($"Python service (PID: {service.Pid}) is healthy on port {service.Port}");

            return service;
        }

        public async Task<bool> WaitForHealthCheck(PythonServiceOptions? options = null, CancellationToken cancellationToken = default)
        {
            options ??= new PythonServiceOptions();

            if (Port <= 0) return true; // no health check if no port

            var url = $"http://localhost:{Port}/health";
            var sw = Stopwatch.StartNew();

            // Use a per-request timeout (e.g., 2 seconds per request)
            var perRequestTimeout = TimeSpan.FromSeconds(Math.Min(2, options.HealthCheckTimeoutSeconds));

            while (sw.Elapsed.TotalSeconds < options.HealthCheckTimeoutSeconds)
            {
                try
                {
                    using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                    cts.CancelAfter(perRequestTimeout);

                    var resp = await client.GetAsync(url, cts.Token);
                    if (resp.IsSuccessStatusCode)
                    {
                        Log.Logger.LogInformation($"Python service (PID: {Pid}) is healthy on port {Port}");
                        return true;
                    }
                }
                catch (OperationCanceledException)
                {
                    // request timed out or cancelled, continue polling
                }
                catch
                {
                    // ignore until timeout
                }

                try
                {
                    await Task.Delay(500, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }

            Log.Logger.LogError($"Python service (PID: {Pid}) failed health check on port {Port} after {options.HealthCheckTimeoutSeconds} seconds.");
            return false;
        }

        public async Task<bool> Stop(PythonServiceOptions? options = null)
        {
            options ??= new PythonServiceOptions();
            try
            {
                var url = $"http://localhost:{Port}/shutdown";
                try
                {
                    await client.GetAsync(url);
                    Log.Logger.LogInformation($"Sent shutdown request to Python service (PID: {Pid}) on port {Port}");
                }
                catch
                {
                    Log.Logger.LogWarning($"Failed to send shutdown request to Python service (PID: {Pid}) on port {Port}");
                }

                // Optionally, wait a bit for graceful shutdown
                await Task.Delay(options.ForceKillTimeoutMilliseconds);

                if (_process != null && !_process.HasExited) // force kill if still running
                {
                    _process.Kill(entireProcessTree: true);
                    _process.WaitForExit();
                    Log.Logger.LogInformation($"Force killed Python service (PID: {Pid})");
                }

                //confirm the port is free
                return PortHelper.checkIfPortIsFree(Port);

            }
            catch
            {
                Log.Logger.LogError($"Error stopping Python service (PID: {Pid})");
                // On any error, check if port is free
                return PortHelper.checkIfPortIsFree(Port);
            }
        }


        public void Dispose() => Stop().GetAwaiter().GetResult();

        public async ValueTask DisposeAsync()
        {
            await Stop();
        }
    }
}

