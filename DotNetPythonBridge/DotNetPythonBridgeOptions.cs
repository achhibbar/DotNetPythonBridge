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
        public TimeSpan CondaDetectionTimeout { get; set; } = TimeSpan.FromSeconds(10); // timeout for detecting conda environments
        public TimeSpan CondaWhichTimeout { get; set; } = TimeSpan.FromSeconds(5); // timeout for which conda executable
        public TimeSpan CondaExecutableCheckTimeout { get; set; } = TimeSpan.FromSeconds(5); // timeout for checking if conda executable is valid
        public TimeSpan CondaListEnvironmentsTimeout { get; set; } = TimeSpan.FromSeconds(10); // timeout for listing conda environments
        public TimeSpan CondaCreateEnvironmentTimeout { get; set; } = TimeSpan.FromMinutes(5); // timeout for creating conda environments
        public TimeSpan CondaDeleteEnvironmentTimeout { get; set; } = TimeSpan.FromMinutes(2); // timeout for deleting conda environments
        public TimeSpan WSL_ListDistrosTimeout { get; set; } = TimeSpan.FromSeconds(10); // timeout for listing WSL distros
        public TimeSpan WSL_WarmupTimeout { get; set; } = TimeSpan.FromSeconds(5); // timeout for warming up WSL distro
        public TimeSpan WSL_DistroDoesFileExistTimeout { get; set; } = TimeSpan.FromSeconds(10); // timeout for checking if file exists in WSL distro
        public TimeSpan WSL_WarmupRetryDelay { get; set; } = TimeSpan.FromSeconds(1); // delay after warming up WSL distro to allow it to settle
        public int WSL_WarmpupRetries { get; set; } = 3; // number of times to retry warming up WSL distro
        public int WSL_GetCondaPathRetries { get; set; } = 3; // number of times to retry getting conda path in WSL distro


        // Fluent helpers
        public DotNetPythonBridgeOptions WithCondaPath(string path) { DefaultCondaPath = path; return this; }
        public DotNetPythonBridgeOptions WithWSLDistro(string distro) { DefaultWSLDistro = distro; return this; }
        /// <summary>
        /// Set the default conda path inside WSL
        /// </summary>
        /// <param name="path"> Path to conda inside WSL, e.g. "/home/username/miniconda3/bin/conda" </param>
        /// <returns></returns>
        public DotNetPythonBridgeOptions WithWSLCondaPath(string path) { DefaultWSLCondaPath = path; return this; }
        public DotNetPythonBridgeOptions WithCondaDetectionTimeout(TimeSpan timeout) { CondaDetectionTimeout = timeout; return this; }
        public DotNetPythonBridgeOptions WithCondaWhichTimeout(TimeSpan timeout) { CondaWhichTimeout = timeout; return this; }
        public DotNetPythonBridgeOptions WithCondaExecutableCheckTimeout(TimeSpan timeout) { CondaExecutableCheckTimeout = timeout; return this; }
        public DotNetPythonBridgeOptions WithCondaListEnvironmentsTimeout(TimeSpan timeout) { CondaListEnvironmentsTimeout = timeout; return this; }
        public DotNetPythonBridgeOptions WithCondaCreateEnvironmentTimeout(TimeSpan timeout) { CondaCreateEnvironmentTimeout = timeout; return this; }
        public DotNetPythonBridgeOptions WithCondaDeleteEnvironmentTimeout(TimeSpan timeout) { CondaDeleteEnvironmentTimeout = timeout; return this; }
        public DotNetPythonBridgeOptions WithWSLListDistrosTimeout(TimeSpan timeout) { WSL_ListDistrosTimeout = timeout; return this; }
        public DotNetPythonBridgeOptions WithWSLWarmpupTimeout(TimeSpan timeout) { WSL_WarmupTimeout = timeout; return this; }
        public DotNetPythonBridgeOptions WithWSLDistroDoesFileExistTimeout(TimeSpan timeout) { WSL_DistroDoesFileExistTimeout = timeout; return this; }

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
        public bool HealthCheckEnabled { get; set; } = true; // by default perform health check after starting service
        public int ServiceRetryCount { get; set; } = 3; // number of times to retry starting the service if health check fails
        // Timeouts for services
        public int HealthCheckTimeoutSeconds { get; set; } = 5; // seconds to wait for health check response
        public int ForceKillTimeoutMilliseconds { get; set; } = 500; // milliseconds to wait after sending kill signal before force killing when stopping the service
        public int StopTimeoutMilliseconds { get; set; } = 2000; // milliseconds to wait for service to stop gracefully

        // Fluent helpers
        public PythonServiceOptions WithPort(int port) { DefaultPort = port; return this; }
        public PythonServiceOptions WithServiceArgs(string args) { DefaultServiceArgs = args; return this; }
        public PythonServiceOptions EnableHealthCheck(bool enabled = true) { HealthCheckEnabled = enabled; return this; }
        public PythonServiceOptions WithServiceRetryCount(int count) { ServiceRetryCount = count; return this; }
        public PythonServiceOptions WithHealthCheckTimeout(int seconds) { HealthCheckTimeoutSeconds = seconds; return this; }
        public PythonServiceOptions WithForceKillTimeout(int milliseconds) { ForceKillTimeoutMilliseconds = milliseconds; return this; }
        public PythonServiceOptions WithStopTimeout(int milliseconds) { StopTimeoutMilliseconds = milliseconds; return this; }
    }
}