using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.Extensions.Logging;

[assembly: InternalsVisibleTo("DotNetPythonBridge.Tests")]
[assembly: InternalsVisibleTo("DotNetPythonBridgeUI")]

namespace DotNetPythonBridge.Utils
{
    internal static class ProcessHelper
    {
        /// <summary>
        /// Use this method to run a short-lived process and wait for it to complete, with support for cancellation.
        /// </summary>
        /// <param name="file"></param>
        /// <param name="args"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        //internal static async Task<PythonResult> RunProcess(string file, string args, CancellationToken cancellationToken = default)
        //{
        //    Log.Logger.LogDebug($"Running process: {file} {args}");

        //    var psi = new ProcessStartInfo
        //    {
        //        FileName = file,
        //        Arguments = args,
        //        RedirectStandardOutput = true,
        //        RedirectStandardError = true,
        //        UseShellExecute = false,
        //        CreateNoWindow = true,
        //        StandardOutputEncoding = GetEncodingForProcess(file, args)

        //    };

        //    using var proc = Process.Start(psi)!;

        //    // Start reading output/error asynchronously
        //    var outputTask = proc.StandardOutput.ReadToEndAsync();
        //    var errorTask = proc.StandardError.ReadToEndAsync();

        //    // Wait for the process to exit (supports cancellation)
        //    await proc.WaitForExitAsync(cancellationToken);

        //    // Ensure all output has been read
        //    await Task.WhenAll(outputTask, errorTask);

        //    Log.Logger.LogDebug($"Process exited with code {proc.ExitCode}");
        //    return new PythonResult(proc.ExitCode, outputTask.Result, errorTask.Result);
        //}


        //internal static Task<Process> StartProcess(
        //    string file,
        //    string args,
        //    CancellationToken cancellationToken = default,
        //    Action<string>? onOutput = null,
        //    Action<string>? onError = null)
        //{
        //    Log.Logger.LogDebug($"Starting process: {file} {args}");

        //    var psi = new ProcessStartInfo
        //    {
        //        FileName = file,
        //        Arguments = args,
        //        RedirectStandardOutput = true,
        //        RedirectStandardError = true,
        //        UseShellExecute = false,
        //        CreateNoWindow = true,
        //        StandardOutputEncoding = GetEncodingForProcess(file, args)
        //    };

        //    var proc = Process.Start(psi)!;

        //    if (proc.HasExited)
        //    {
        //        Log.Logger.LogError("Process exited prematurely.");
        //        throw new Exception("Process exited prematurely.");
        //    }

        //    // Hook output/error events to consume buffers
        //    proc.OutputDataReceived += (s, e) =>
        //    {
        //        if (e.Data != null)
        //        {
        //            onOutput?.Invoke(e.Data);
        //            Log.Logger.LogDebug($"[Process STDOUT] {e.Data}");
        //        }
        //    };
        //    proc.ErrorDataReceived += (s, e) =>
        //    {
        //        if (e.Data != null)
        //        {
        //            onError?.Invoke(e.Data);
        //            Log.Logger.LogWarning($"[Process STDERR] {e.Data}");
        //        }
        //    };
        //    proc.BeginOutputReadLine();
        //    proc.BeginErrorReadLine();

        //    if (cancellationToken != default)
        //    {
        //        cancellationToken.Register(() =>
        //        {
        //            if (!proc.HasExited)
        //            {
        //                Log.Logger.LogInformation("Cancellation requested. Killing process.");
        //                try { proc.Kill(entireProcessTree: true); } catch { Log.Logger.LogWarning("Failed to kill process."); }
        //            }
        //        });
        //    }

        //    Log.Logger.LogDebug($"Process started with PID {proc.Id}");
        //    return Task.FromResult(proc);
        //}

        /// <summary>
        /// Get the appropriate encoding for the process based on the OS and command being run.
        /// </summary>
        /// <param name="file"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        private static Encoding GetEncodingForProcess(string file, string args)
        {
            // If running WSL bash on Windows, use UTF-8 encoding. This is to get conda/mamba paths correctly for wsl distros.
            // This is also used when running python scripts or code in WSL on Windows.
            if (OperatingSystem.IsWindows() && file.ToLower().Contains("wsl") && args.ToLower().Contains("bash"))
            {
                return Encoding.UTF8;
            }
            // Windows + conda info
            else if (OperatingSystem.IsWindows() && args.ToLower().Contains("info"))
            {
                return Encoding.GetEncoding("utf-8"); // Use UTF-8 and not Encoding.UTF8 to avoid BOM issues
                //return Encoding.UTF8;
            }
            //when running a python script on windows, the output is in utf-8
            else if (OperatingSystem.IsWindows() && file.ToLower().Contains("python") && args.ToLower().Contains(".py"))
            {
                return Encoding.UTF8;
            }
            // when running python code on windows, the output is in utf-8
            else if (OperatingSystem.IsWindows() && file.ToLower().Contains("python") && args.ToLower().Contains("-c"))
            {
                return Encoding.UTF8;
            }
            // else if (OperatingSystem.IsWindows() && getting a list of wsl distros
            else if (OperatingSystem.IsWindows() && file.ToLower().Contains("wsl") && args.ToLower().Contains("-l"))
            {
                return Encoding.Unicode;
            }
            // Windows
            else if (OperatingSystem.IsWindows())
            {
                return Encoding.UTF8;
            }
            // Linux and MacOS
            else
            {
                return Encoding.UTF8;
            }
        }

        internal static Task<PythonResult> RunProcess(
            string file,
            IEnumerable<string> arguments,
            CancellationToken cancellationToken = default)
        {
            var psi = new ProcessStartInfo
            {
                FileName = file,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            foreach (var arg in arguments)
                psi.ArgumentList.Add(arg);

            return RunProcessInternal(psi, cancellationToken);
        }

        private static async Task<PythonResult> RunProcessInternal(
            ProcessStartInfo psi,
            CancellationToken cancellationToken)
        {
            Log.Logger.LogDebug($"Running process: {psi.FileName} {psi.Arguments}");

            using var proc = Process.Start(psi)
                ?? throw new Exception("Failed to start process.");

            // Kick off parallel reading of output/error
            Task<string> outputTask = proc.StandardOutput.ReadToEndAsync();
            Task<string> errorTask = proc.StandardError.ReadToEndAsync();

            try
            {
                // Wait for process to exit (supports cancellation)
                await proc.WaitForExitAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                try { proc.Kill(entireProcessTree: true); } catch { }

                throw;
            }

            // Ensure all output is read
            await Task.WhenAll(outputTask, errorTask);

            var result = new PythonResult(
                proc.ExitCode,
                outputTask.Result,
                errorTask.Result);

            Log.Logger.LogDebug($"Process exited with code {result.ExitCode}");

            return result;
        }

        internal static Task<Process> StartProcess(
            string file,
            IEnumerable<string> arguments,
            CancellationToken cancellationToken = default,
            Action<string>? onOutput = null,
            Action<string>? onError = null)
        {
            var psi = new ProcessStartInfo
            {
                FileName = file,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            foreach (var arg in arguments)
                psi.ArgumentList.Add(arg);

            return StartProcessInternal(psi, cancellationToken, onOutput, onError);
        }

        private static Task<Process> StartProcessInternal(
            ProcessStartInfo psi,
            CancellationToken cancellationToken,
            Action<string>? onOutput,
            Action<string>? onError)
        {
            var proc = Process.Start(psi)!;

            if (proc.HasExited)
                throw new Exception("Process exited prematurely.");

            proc.OutputDataReceived += (s, e) =>
            {
                if (e.Data != null)
                {
                    onOutput?.Invoke(e.Data);
                    Log.Logger.LogDebug($"[STDOUT] {e.Data}");
                }
            };

            proc.ErrorDataReceived += (s, e) =>
            {
                if (e.Data != null)
                {
                    onError?.Invoke(e.Data);
                    Log.Logger.LogWarning($"[STDERR] {e.Data}");
                }
            };

            proc.BeginOutputReadLine();
            proc.BeginErrorReadLine();

            if (cancellationToken.CanBeCanceled)
            {
                cancellationToken.Register(() =>
                {
                    if (!proc.HasExited)
                    {
                        Log.Logger.LogInformation("Cancellation requested. Killing process.");
                        try { proc.Kill(entireProcessTree: true); }
                        catch { }
                    }
                });
            }

            return Task.FromResult(proc);
        }


        /// <summary>
        /// Potential wrapper for RunProcess with string args, that will take over in future if needed
        /// </summary>
        /// <param name="file"></param>
        /// <param name="args"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        internal static Task<PythonResult> RunProcess(
            string file,
            string args,
            CancellationToken cancellationToken = default)
        {
            var psi = new ProcessStartInfo
            {
                FileName = file,
                Arguments = args,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = GetEncodingForProcess(file, args)
            };

            return RunProcessInternal(psi, cancellationToken);
        }

        /// <summary>
        /// Potential wrapper for StartProcess with string args, that will take over in future if needed
        internal static Task<Process> StartProcess(
            string file,
            string args,
            CancellationToken cancellationToken = default,
            Action<string>? onOutput = null,
            Action<string>? onError = null)
        {
            return StartProcessInternal(
                new ProcessStartInfo
                {
                    FileName = file,
                    Arguments = args,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    StandardOutputEncoding = GetEncodingForProcess(file, args)
                },
                cancellationToken,
                onOutput,
                onError
            );
        }


    }
}
