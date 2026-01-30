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
    /// <summary>
    /// Wrapper handle for managing the lifecycle of a PythonService.
    /// </summary>
    public class PythonServiceHandle : IAsyncDisposable, IDisposable
    {
        /// <summary>
        /// The underlying Python service.
        /// </summary>
        public PythonService Service { get; }

        /// <summary>
        /// Handle for managing the lifecycle of a PythonService.
        /// </summary>
        /// <param name="service"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public PythonServiceHandle(PythonService? service)
        {
            Service = service ?? throw new ArgumentNullException(nameof(service));
        }

        /// <summary>
        /// Dispose the underlying Python service.
        /// </summary>
        public void Dispose() => Service?.Dispose();

        /// <summary>
        /// Asynchronously dispose the underlying Python service.
        /// </summary>
        /// <returns></returns>
        public async ValueTask DisposeAsync() => await (Service?.DisposeAsync() ?? ValueTask.CompletedTask);
    }

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


        public static async Task<PythonServiceHandle> Start(string scriptPath, PythonEnvironment? env = null, PythonServiceOptions? options = null,
            CancellationToken cancellationToken = default, TimeSpan? timeout = null)
        {
            Log.Logger.LogDebug($"Starting Python service using environment: {(env != null ? env.Name : "Base")}, script: {scriptPath}");

            options ??= new PythonServiceOptions(); // use default options if none provided

            if (!File.Exists(scriptPath))
            {
                Log.Logger.LogError($"Script not found: {scriptPath}");
                throw new FileNotFoundException($"Script not found: {scriptPath}");
            }

            // Resolve python executable inside the env
            string pythonExe = await PythonRunner.GetPythonExecutable(env);

            // if the service fails to become healthy, retry up to 3 times
            for (int i = 0; i < options.ServiceRetryCount; i++)
            {
                // Reserve port, if 0 then get free port, otherwise use specified port
                var portReservation = PortHelper.ReservePort(options.DefaultPort);
                int port = portReservation.Port;
                //int port = options.DefaultPort == 0 ? PortHelper.GetFreePort() : options.DefaultPort; // if 0, get free port

                // Arguments: script + port + user args, using bash escaping if needed
                string[] args = { scriptPath, "--port", port.ToString(), BashCommandBuilder.BashEscape(BashCommandBuilder.Escape(options.DefaultServiceArgs)) };
                //string[] args = { scriptPath, "--port", port.ToString(), options.DefaultServiceArgs };

                // give up the port reservation
                portReservation.Release();

                // start the process using ArgumentList to avoid issues with spaces
                var proc = await ProcessHelper.StartProcess(pythonExe, args, cancellationToken: cancellationToken, timeout: timeout);

                var service = new PythonService(proc, port); // create service instance

                // Optional health check
                if (options.HealthCheckEnabled && !await service.WaitForHealthCheck(options))
                {
                    // on failure, stop and retry
                    await service.Stop();
                    Log.Logger.LogWarning($"Python service (PID: {service.Pid}) failed health check on port {service.Port}. Retrying...");
                    continue; // try again
                }
                Log.Logger.LogInformation($"Python service (PID: {service.Pid}) is healthy on port {service.Port}");
                return new PythonServiceHandle(service);
            }
            throw new InvalidOperationException($"Python service failed to become healthy after {options.ServiceRetryCount} attempts.");
        }


        /// <summary>
        /// Start a long-running Python service inside the given conda environment.
        /// </summary>
        /// <param name="scriptPath"></param>
        /// <param name="env"></param>
        /// <param name="options"></param>
        /// <param name="wsl"></param>
        /// <returns></returns>
        /// <exception cref="FileNotFoundException"></exception>
        /// <exception cref="Exception"></exception>
        public static async Task<PythonServiceHandle> StartWSL(
            string scriptPath,
            PythonEnvironment? env = null,
            PythonServiceOptions? options = null,
            WSL_Helper.WSL_Distro? wsl = null,
            CancellationToken cancellationToken = default,
            TimeSpan? timeout = null)
        {
            Log.Logger.LogDebug(
                $"Starting Python service using env: {env?.Name ?? "Base"}, script: {scriptPath}, WSL: {wsl?.Name ?? "None"}");

            if (wsl == null)
            {
                wsl = await WSL_Helper.getDefaultWSL_Distro();
                Log.Logger.LogDebug($"No WSL distro specified. Using default: {wsl.Name}");
            }

            options ??= new PythonServiceOptions();

            if (!File.Exists(scriptPath))
                throw new FileNotFoundException($"Script not found: {scriptPath}");

            // Resolve python path inside WSL
            string pythonExe = await PythonRunner.GetPythonExecutableWSL(env, wsl);
            string wslScriptPath = FilenameHelper.convertWindowsPathToWSL(scriptPath);

            // if the service fails to become healthy, retry up to 3 times
            for (int i = 0; i < options.ServiceRetryCount; i++)
            {
                // Reserve port
                var portReservation = PortHelper.ReservePort(options.DefaultPort);
                int port = portReservation.Port;

                // Build the inner bash command safely
                string bashCommand = BashCommandBuilder.BuildBashStartServiceCommand(pythonExe, wslScriptPath, port, options);

                // Free port reservation after building bashCommand
                portReservation.Release();

                // Start process via WSL using ArgumentList
                var proc = await ProcessHelper.StartProcess("wsl", new[] { "-d", wsl.Name, "bash", "-lc", bashCommand },
                    cancellationToken: cancellationToken, timeout: timeout);

                var service = new PythonService(proc, port, wsl);

                Log.Logger.LogDebug(
                    $"Started Python service in WSL '{wsl.Name}' (PID: {service.Pid}) on port {port}");

                // Optional health check
                if (options.HealthCheckEnabled && !await service.WaitForHealthCheck(options))
                {
                    // on failure, stop and retry
                    await service.Stop();
                    Log.Logger.LogWarning($"Python service in WSL '{wsl.Name}' (PID: {service.Pid}) failed health check on port {port}. Retrying...");
                    continue; // try again
                }

                Log.Logger.LogInformation(
                    $"Python service (PID: {service.Pid}) is healthy on port {port}");

                return new PythonServiceHandle(service);
            }
            throw new InvalidOperationException($"Python service failed to become healthy after {options.ServiceRetryCount} attempts.");
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
                catch (HttpRequestException)
                {
                    // network error, ignore until timeout
                }

                try
                {
                    await Task.Delay(options.HealthCheckRetryDelayMilliseconds, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }

            Log.Logger.LogError($"Python service (PID: {Pid}) failed health check on port {Port} after {options.HealthCheckTimeoutSeconds} seconds.");
            return false;
        }

        /// <summary>
        /// Stop the Python service gracefully, with optional force kill after timeout.
        /// </summary>
        /// <param name="options"></param>
        /// <returns></returns>
        public async Task<bool> Stop(PythonServiceOptions? options = null)
        {
            options ??= new PythonServiceOptions();
            bool shutdownRequestSent = false;
            try
            {
                var url = $"http://localhost:{Port}/shutdown";
                try
                {
                    await client.GetAsync(url);
                    shutdownRequestSent = true;
                }
                catch
                {
                    // Only log if shutdown request fails
                    Log.Logger.LogWarning($"Failed to send shutdown request to Python service (PID: {Pid}) on port {Port}");
                }

                // Only log once for shutdown request
                if (shutdownRequestSent)
                    Log.Logger.LogInformation($"Sent shutdown request to Python service (PID: {Pid}) on port {Port}");

                // poll for exit using options.ForceKillTimeoutMilliseconds as total timeout
                var sw = Stopwatch.StartNew();
                while (sw.Elapsed.TotalMilliseconds < options.ForceKillTimeoutMilliseconds)
                {
                    if (_process == null || _process.HasExited ) // check if process has exited
                    {
                        if (PortHelper.checkIfPortIsFree(Port)) // check if port is free
                        {
                            Log.Logger.LogInformation($"Python service (PID: {Pid}) has exited gracefully.");
                            return true;
                        }
                    }
                    await Task.Delay(options.ProcessStoppedCheckDelayMilliseconds);
                }

                // _process is still running after timeout, force kill
                if (_process != null && !_process.HasExited) // force kill if still running
                {
                    _process.Kill(entireProcessTree: true);
                    
                    var waitTask = _process.WaitForExitAsync();
                    if (await Task.WhenAny(waitTask, Task.Delay(options.StopTimeoutMilliseconds)) == waitTask) // wait for exit with timeout
                    {
                        Log.Logger.LogInformation($"Force killed Python service (PID: {Pid})");
                    }
                    else // timeout
                    {
                        Log.Logger.LogWarning($"Process (PID: {Pid}) did not exit within {options.StopTimeoutMilliseconds} ms after kill.");
                    }
                }

                // Dispose of the process and set it to null
                _process?.Dispose();
                _process = null;

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

