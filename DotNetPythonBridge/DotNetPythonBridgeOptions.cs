using Microsoft.Extensions.Logging;
using DotNetPythonBridge.Utils;

namespace DotNetPythonBridge
{
    public class DotNetPythonBridgeOptions
    {
        // Paths & environment
        public string? DefaultCondaPath { get; set; } // windows path to conda executable e.g. "C:\Users\username\miniconda3\Scripts\conda.exe"
        public string? DefaultWSLDistro { get; set; } // name of the WSL distribution to use, e.g. "Ubuntu-20.04"
        public string? DefaultWSLCondaPath { get; set; } // linux path to conda executable inside WSL e.g. "/home/username/miniconda3/bin/conda"
        public TimeSpan CondaDetectionTimeout { get; set; } = TimeSpan.FromSeconds(10);
        public TimeSpan CondaWhichTimeout { get; set; } = TimeSpan.FromSeconds(5);
        public TimeSpan CondaExecutableCheckTimeout { get; set; } = TimeSpan.FromSeconds(5);
        public TimeSpan CondaListEnvironmentsTimeout { get; set; } = TimeSpan.FromSeconds(10);
        public TimeSpan CondaCreateEnvironmentTimeout { get; set; } = TimeSpan.FromMinutes(5);
        public TimeSpan CondaDeleteEnvironmentTimeout { get; set; } = TimeSpan.FromMinutes(2);
        public TimeSpan WSLListDistrosTimeout { get; set; } = TimeSpan.FromSeconds(10);
        public TimeSpan WSLWarmupTimeout { get; set; } = TimeSpan.FromSeconds(5);
        public TimeSpan WSLDistroDoesFileExistTimeout { get; set; } = TimeSpan.FromSeconds(10);
        public TimeSpan WSLWarmupRetryDelay { get; set; } = TimeSpan.FromSeconds(1);
        public int WSLWarmupRetries { get; set; } = 3;
        public int WSLGetCondaPathRetries { get; set; } = 3;

        // Fluent helpers
        /// <summary>
        /// Sets the default conda executable path.
        /// </summary>
        /// <param name="path">The path to the conda executable.</param>
        /// <returns>The current instance of <see cref="DotNetPythonBridgeOptions"/>.</returns>
        public DotNetPythonBridgeOptions WithCondaPath(string path) { DefaultCondaPath = path; return this; }
        /// <summary>
        /// Sets the default WSL distribution name.
        /// </summary>
        /// <param name="distro">The name of the WSL distribution.</param>
        /// <returns>The current instance of <see cref="DotNetPythonBridgeOptions"/>.</returns>
        public DotNetPythonBridgeOptions WithWSLDistro(string distro) { DefaultWSLDistro = distro; return this; }
        /// <summary>
        /// Sets the default conda path inside WSL. Can be WSL path e.g. "/home/username/miniconda3/bin/conda".
        /// Or the equivalent Windows path e.g. @"\\wsl$\Ubuntu-20.04\home\username\miniconda3\bin\conda".
        /// </summary>
        /// <param name="path">The path to conda inside WSL.</param>
        /// <returns>The current instance of <see cref="DotNetPythonBridgeOptions"/>.</returns>
        public DotNetPythonBridgeOptions WithWSLCondaPath(string path) { DefaultWSLCondaPath = path; return this; }
        /// <summary>
        /// Sets the timeout for conda detection.
        /// </summary>
        /// <param name="timeout">The timeout duration.</param>
        /// <returns>The current instance of <see cref="DotNetPythonBridgeOptions"/>.</returns>
        public DotNetPythonBridgeOptions WithCondaDetectionTimeout(TimeSpan timeout) { CondaDetectionTimeout = timeout; return this; }
        /// <summary>
        /// Sets the timeout for the 'conda which' command.
        /// </summary>
        /// <param name="timeout">The timeout duration.</param>
        /// <returns>The current instance of <see cref="DotNetPythonBridgeOptions"/>.</returns>
        public DotNetPythonBridgeOptions WithCondaWhichTimeout(TimeSpan timeout) { CondaWhichTimeout = timeout; return this; }
        /// <summary>
        /// Sets the timeout for checking the conda executable.
        /// </summary>
        /// <param name="timeout">The timeout duration.</param>
        /// <returns>The current instance of <see cref="DotNetPythonBridgeOptions"/>.</returns>
        public DotNetPythonBridgeOptions WithCondaExecutableCheckTimeout(TimeSpan timeout) { CondaExecutableCheckTimeout = timeout; return this; }
        /// <summary>
        /// Sets the timeout for listing conda environments.
        /// </summary>
        /// <param name="timeout">The timeout duration.</param>
        /// <returns>The current instance of <see cref="DotNetPythonBridgeOptions"/>.</returns>
        public DotNetPythonBridgeOptions WithCondaListEnvironmentsTimeout(TimeSpan timeout) { CondaListEnvironmentsTimeout = timeout; return this; }
        /// <summary>
        /// Sets the timeout for creating a conda environment.
        /// </summary>
        /// <param name="timeout">The timeout duration.</param>
        /// <returns>The current instance of <see cref="DotNetPythonBridgeOptions"/>.</returns>
        public DotNetPythonBridgeOptions WithCondaCreateEnvironmentTimeout(TimeSpan timeout) { CondaCreateEnvironmentTimeout = timeout; return this; }
        /// <summary>
        /// Sets the timeout for deleting a conda environment.
        /// </summary>
        /// <param name="timeout">The timeout duration.</param>
        /// <returns>The current instance of <see cref="DotNetPythonBridgeOptions"/>.</returns>
        public DotNetPythonBridgeOptions WithCondaDeleteEnvironmentTimeout(TimeSpan timeout) { CondaDeleteEnvironmentTimeout = timeout; return this; }
        /// <summary>
        /// Sets the timeout for listing WSL distributions.
        /// </summary>
        /// <param name="timeout">The timeout duration.</param>
        /// <returns>The current instance of <see cref="DotNetPythonBridgeOptions"/>.</returns>
        public DotNetPythonBridgeOptions WithWSLListDistrosTimeout(TimeSpan timeout) { WSLListDistrosTimeout = timeout; return this; }
        /// <summary>
        /// Sets the timeout for WSL warmup.
        /// </summary>
        /// <param name="timeout">The timeout duration.</param>
        /// <returns>The current instance of <see cref="DotNetPythonBridgeOptions"/>.</returns>
        public DotNetPythonBridgeOptions WithWSLWarmupTimeout(TimeSpan timeout) { WSLWarmupTimeout = timeout; return this; }
        /// <summary>
        /// Sets the timeout for checking if a file exists in the WSL distribution.
        /// </summary>
        /// <param name="timeout">The timeout duration.</param>
        /// <returns>The current instance of <see cref="DotNetPythonBridgeOptions"/>.</returns>
        public DotNetPythonBridgeOptions WithWSLDistroDoesFileExistTimeout(TimeSpan timeout) { WSLDistroDoesFileExistTimeout = timeout; return this; }
        /// <summary>
        /// Sets the retry delay for WSL warmup.
        /// </summary>
        /// <param name="delay">The delay duration.</param>
        /// <returns>The current instance of <see cref="DotNetPythonBridgeOptions"/>.</returns>
        public DotNetPythonBridgeOptions WithWSLWarmupRetryDelay(TimeSpan delay) { WSLWarmupRetryDelay = delay; return this; }
        /// <summary>
        /// Sets the number of retries for WSL warmup.
        /// </summary>
        /// <param name="retries">The number of retries.</param>
        /// <returns>The current instance of <see cref="DotNetPythonBridgeOptions"/>.</returns>
        public DotNetPythonBridgeOptions WithWSLWarmupRetries(int retries) { WSLWarmupRetries = retries; return this; }
        /// <summary>
        /// Sets the number of retries for getting the conda path in WSL.
        /// </summary>
        /// <param name="retries">The number of retries.</param>
        /// <returns>The current instance of <see cref="DotNetPythonBridgeOptions"/>.</returns>
        public DotNetPythonBridgeOptions WithWSLGetCondaPathRetries(int retries) { WSLGetCondaPathRetries = retries; return this; }

        // return a WSL_Helper.WSL_Distro object if DefaultWSLDistro is set
        public async Task<Utils.WSL_Helper.WSL_Distro?> GetWSL_DistroAsync()
        {
            if (string.IsNullOrWhiteSpace(DefaultWSLDistro))
                return null;

            var distros = await Utils.WSL_Helper.GetWSLDistros();
            var distro = distros.Distros.FirstOrDefault(d => d.Name.Equals(DefaultWSLDistro, StringComparison.OrdinalIgnoreCase));
            if (distro == null)
            {
                Log.Logger.LogError($"Specified WSL distribution not found: {DefaultWSLDistro}");
                throw new Exception($"Specified WSL distribution not found: {DefaultWSLDistro}");
            }
            return distro;
        }
    }

    public class PythonServiceOptions
    {
        // Service defaults
        public int DefaultPort { get; set; } = 0; // 0 = auto
        public string DefaultServiceArgs { get; set; } = "--host 127.0.0.1";
        public bool HealthCheckEnabled { get; set; } = true;
        public int ServiceRetryCount { get; set; } = 3;

        // Timeouts and delays (all as TimeSpan)
        public TimeSpan HealthCheckTimeout { get; set; } = TimeSpan.FromSeconds(5);
        public TimeSpan ForceKillTimeout { get; set; } = TimeSpan.FromMilliseconds(500);
        public TimeSpan StopTimeout { get; set; } = TimeSpan.FromMilliseconds(2000);
        public TimeSpan HealthCheckRetryDelay { get; set; } = TimeSpan.FromMilliseconds(500);
        public TimeSpan ProcessStoppedCheckDelay { get; set; } = TimeSpan.FromMilliseconds(200);

        /// <summary>
        /// Sets the default port for the Python service. A value of 0 means the port will be assigned automatically.
        /// </summary>
        /// <param name="port">The port number to set.</param>
        /// <returns>The current instance of <see cref="PythonServiceOptions"/>.</returns>
        public PythonServiceOptions WithPort(int port) { DefaultPort = port; return this; }
        /// <summary>
        /// Sets the default arguments for the Python service. These arguments will be passed when starting the service.
        /// </summary>
        /// <param name="args">The service arguments to set.</param>
        /// <returns>The current instance of <see cref="PythonServiceOptions"/>.</returns>
        public PythonServiceOptions WithServiceArgs(string args) { DefaultServiceArgs = args; return this; }
        /// <summary>
        /// Enables or disables health checks for the Python service. Health checks help ensure the service is running correctly.
        /// </summary>
        /// <param name="enabled">True to enable health checks; otherwise, false.</param>
        /// <returns>The current instance of <see cref="PythonServiceOptions"/>.</returns>
        public PythonServiceOptions EnableHealthCheck(bool enabled = true) { HealthCheckEnabled = enabled; return this; }
        /// <summary>
        /// Sets the number of retries for the Python service in case of failure. This helps in making the service more resilient.
        /// </summary>
        /// <param name="count">The number of retry attempts to set.</param>
        /// <returns>The current instance of <see cref="PythonServiceOptions"/>.</returns>
        public PythonServiceOptions WithServiceRetryCount(int count) { ServiceRetryCount = count; return this; }
        /// <summary>
        /// Sets the timeout duration for health checks. This defines how long to wait for a health check response before timing out.
        /// </summary>
        /// <param name="timeout">The timeout duration to set.</param>
        /// <returns>The current instance of <see cref="PythonServiceOptions"/>.</returns>
        public PythonServiceOptions WithHealthCheckTimeout(TimeSpan timeout) { HealthCheckTimeout = timeout; return this; }
        /// <summary>
        /// Sets the timeout duration for forcefully killing the Python service. This is used when the service does not respond.
        /// </summary>
        /// <param name="timeout">The timeout duration to set.</param>
        /// <returns>The current instance of <see cref="PythonServiceOptions"/>.</returns>
        public PythonServiceOptions WithForceKillTimeout(TimeSpan timeout) { ForceKillTimeout = timeout; return this; }
        /// <summary>
        /// Sets the timeout duration for stopping the Python service gracefully. This allows the service to shut down properly.
        /// </summary>
        /// <param name="timeout">The timeout duration to set.</param>
        /// <returns>The current instance of <see cref="PythonServiceOptions"/>.</returns>
        public PythonServiceOptions WithStopTimeout(TimeSpan timeout) { StopTimeout = timeout; return this; }
        /// <summary>
        /// Sets the delay duration between health check retries. This helps in managing the frequency of health checks.
        /// </summary>
        /// <param name="delay">The delay duration to set.</param>
        /// <returns>The current instance of <see cref="PythonServiceOptions"/>.</returns>
        public PythonServiceOptions WithHealthCheckRetryDelay(TimeSpan delay) { HealthCheckRetryDelay = delay; return this; }
        /// <summary>
        /// Sets the delay duration for checking if the process has stopped. This helps in ensuring the process is monitored effectively.
        /// </summary>
        /// <param name="delay">The delay duration to set.</param>
        /// <returns>The current instance of <see cref="PythonServiceOptions"/>.</returns>
        public PythonServiceOptions WithProcessStoppedCheckDelay(TimeSpan delay) { ProcessStoppedCheckDelay = delay; return this; }
    }
}