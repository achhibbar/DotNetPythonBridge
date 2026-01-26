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
        /// Get the appropriate encoding for the process based on the OS and command being run.
        /// </summary>
        /// <param name="file"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        //private static Encoding GetEncodingForProcess(string file, string args)
        //{
        //    // If running WSL bash on Windows, use UTF-8 encoding. This is to get conda/mamba paths correctly for wsl distros.
        //    // This is also used when running python scripts or code in WSL on Windows.
        //    if (OperatingSystem.IsWindows() && file.ToLower().Contains("wsl") && args.ToLower().Contains("bash"))
        //    {
        //        return Encoding.UTF8;
        //    }
        //    // Windows + conda info
        //    else if (OperatingSystem.IsWindows() && args.ToLower().Contains("info"))
        //    {
        //        return Encoding.GetEncoding("utf-8"); // Use UTF-8 and not Encoding.UTF8 to avoid BOM issues
        //        //return Encoding.UTF8;
        //    }
        //    //when running a python script on windows, the output is in utf-8
        //    else if (OperatingSystem.IsWindows() && file.ToLower().Contains("python") && args.ToLower().Contains(".py"))
        //    {
        //        return Encoding.UTF8;
        //    }
        //    // when running python code on windows, the output is in utf-8
        //    else if (OperatingSystem.IsWindows() && file.ToLower().Contains("python") && args.ToLower().Contains("-c"))
        //    {
        //        return Encoding.UTF8;
        //    }
        //    // else if (OperatingSystem.IsWindows() && getting a list of wsl distros
        //    else if (OperatingSystem.IsWindows() && file.ToLower().Contains("wsl") && args.ToLower().Contains("-l"))
        //    {
        //        return Encoding.Unicode;
        //    }
        //    // Windows
        //    else if (OperatingSystem.IsWindows())
        //    {
        //        return Encoding.UTF8;
        //    }
        //    // Linux and MacOS
        //    else
        //    {
        //        return Encoding.UTF8;
        //    }
        //}

        /// <summary>
        /// Runner for processes with arg list
        /// </summary>
        /// <param name="file"></param>
        /// <param name="arguments"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        internal static Task<PythonResult> RunProcess(
            string file,
            IEnumerable<string> arguments,
            CancellationToken cancellationToken = default,
            TimeSpan? timeout = null,
            Encoding? encoding = null) // Changed to nullable
        {
            // All calls to this overload use UTF-8
            // Unicode is only used when getting WSL distros via the string args overload in the method below
            encoding ??= Encoding.UTF8; // Assign default value if null

            var psi = new ProcessStartInfo
            {
                FileName = file,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = encoding
            };

            foreach (var arg in arguments)
                psi.ArgumentList.Add(arg);

            return RunProcessInternal(psi, cancellationToken, timeout);
        }

        /// <summary>
        /// Wrapper for RunProcess with string args
        /// </summary>
        /// <param name="file"></param>
        /// <param name="args"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        internal static Task<PythonResult> RunProcess(
            string file,
            string args,
            CancellationToken cancellationToken = default,
            TimeSpan? timeout = null,
            Encoding? encoding = null) // Changed to nullable
        {
            // The only time encoding is null is when called from WSL_Helper.GetWSLDistros
            // which passes Encoding.Unicode
            // In all other cases, we want to default to UTF-8
            encoding ??= Encoding.UTF8; // Assign default value if null

            var psi = new ProcessStartInfo
            {
                FileName = file,
                Arguments = args,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = encoding
                //StandardOutputEncoding = GetEncodingForProcess(file, args)
            };

            return RunProcessInternal(psi, cancellationToken, timeout);
        }

        /// <summary>
        /// Start and run a process, capturing output and error.
        /// </summary>
        /// <param name="psi"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        private static async Task<PythonResult> RunProcessInternal(
            ProcessStartInfo psi,
            CancellationToken cancellationToken,
            TimeSpan? timeout = null)
        {
            Log.Logger.LogDebug($"Running process: {psi.FileName} {psi.Arguments}");

            using var proc = Process.Start(psi)
                ?? throw new Exception("Failed to start process.");

            Task<string> outputTask = proc.StandardOutput.ReadToEndAsync();
            Task<string> errorTask = proc.StandardError.ReadToEndAsync();

            CancellationTokenSource? timeoutCts = null;
            CancellationToken linkedToken = cancellationToken;

            if (timeout.HasValue) // if a timeout is specified then link it with the cancellation token
            {
                timeoutCts = new CancellationTokenSource(timeout.Value);
                linkedToken = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token).Token;
            }

            using (linkedToken.Register(() =>
            {
                if (!proc.HasExited)
                {
                    try { proc.Kill(entireProcessTree: true); } catch { }
                }
            }))
            {
                try
                {
                    await proc.WaitForExitAsync(linkedToken);
                }
                catch (OperationCanceledException)
                {
                    if (timeoutCts != null && timeoutCts.IsCancellationRequested)
                        throw new TimeoutException("The process has timed out.");
                    throw;
                }
            }

            await Task.WhenAll(outputTask, errorTask);

            var result = new PythonResult(
                proc.ExitCode,
                outputTask.Result,
                errorTask.Result);

            Log.Logger.LogDebug($"Process exited with code {result.ExitCode}");

            return result;
        }

        /// <summary>
        /// Starter for processes with arg list used for long-running processes
        /// </summary>
        /// <param name="file"></param>
        /// <param name="arguments"></param>
        /// <param name="cancellationToken"></param>
        /// <param name="onOutput"></param>
        /// <param name="onError"></param>
        /// <returns></returns>
        internal static Task<Process> StartProcess(
            string file,
            IEnumerable<string> arguments,
            CancellationToken cancellationToken = default,
            TimeSpan? timeout = null,
            Action<string>? onOutput = null,
            Action<string>? onError = null,
            Encoding? encoding = null)
        {
            // All calls to this overload use UTF-8
            // This is called when starting a python service in Windows or WSL in PythonService class
            encoding ??= Encoding.UTF8;

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

            return StartProcessInternal(psi, cancellationToken, onOutput, onError, timeout);
        }

        /// <summary>
        /// Wrapper for StartProcess with string args used for long-running processes
        internal static Task<Process> StartProcess(
            string file,
            string args,
            CancellationToken cancellationToken = default,
            TimeSpan? timeout = null,
            Action<string>? onOutput = null,
            Action<string>? onError = null,
            Encoding? encoding = null)
        {
            encoding ??= Encoding.UTF8;

            return StartProcessInternal(
                new ProcessStartInfo
                {
                    FileName = file,
                    Arguments = args,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    StandardOutputEncoding = encoding
                    //StandardOutputEncoding = GetEncodingForProcess(file, args)
                },
                cancellationToken,
                onOutput,
                onError,
                timeout
            );
        }

        /// <summary>
        /// Start and run a process, capturing output and error via callbacks. Used for long-running processes.
        /// </summary>
        /// <param name="psi"></param>
        /// <param name="cancellationToken"></param>
        /// <param name="onOutput"></param>
        /// <param name="onError"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        private static Task<Process> StartProcessInternal(
            ProcessStartInfo psi,
            CancellationToken cancellationToken,
            Action<string>? onOutput,
            Action<string>? onError,
            TimeSpan? timeout = null)
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

            // Register cancellation to kill the process
            CancellationTokenSource? timeoutCts = null;
            CancellationToken linkedToken = cancellationToken;

            if (timeout.HasValue) // if a timeout is specified then link it with the cancellation token
            {
                timeoutCts = new CancellationTokenSource(timeout.Value);
                linkedToken = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token).Token;
            }

            linkedToken.Register(() =>
            {
                if (!proc.HasExited)
                {
                    Log.Logger.LogInformation("Cancellation or timeout requested. Killing process.");
                    try { proc.Kill(entireProcessTree: true); }
                    catch { }
                }
            });

            // Return immediately; do not await process exit
            return Task.FromResult(proc);
        }


    }
}
