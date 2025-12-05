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

        // Fluent helpers
        public DotNetPythonBridgeOptions WithCondaPath(string path) { DefaultCondaPath = path; return this; }
        public DotNetPythonBridgeOptions WithWSLDistro(string distro) { DefaultWSLDistro = distro; return this; }
        /// <summary>
        /// Set the default conda path inside WSL
        /// </summary>
        /// <param name="path"> Path to conda inside WSL, e.g. "/home/username/miniconda3/bin/conda" </param>
        /// <returns></returns>
        public DotNetPythonBridgeOptions WithWSLCondaPath(string path) { DefaultWSLCondaPath = path; return this; }

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

        // Timeouts
        public int HealthCheckTimeoutSeconds { get; set; } = 5;
        public int ForceKillTimeoutMilliseconds { get; set; } = 500;
        public int StopTimeoutMilliseconds { get; set; } = 2000;

        // Fluent helpers
        public PythonServiceOptions WithPort(int port) { DefaultPort = port; return this; }
        public PythonServiceOptions WithServiceArgs(string args) { DefaultServiceArgs = args; return this; }
        public PythonServiceOptions EnableHealthCheck(bool enabled = true) { HealthCheckEnabled = enabled; return this; }
        public PythonServiceOptions WithHealthCheckTimeout(int seconds) { HealthCheckTimeoutSeconds = seconds; return this; }
        public PythonServiceOptions WithForceKillTimeout(int milliseconds) { ForceKillTimeoutMilliseconds = milliseconds; return this; }
        public PythonServiceOptions WithStopTimeout(int milliseconds) { StopTimeoutMilliseconds = milliseconds; return this; }
    }
}