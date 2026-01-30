using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace DotNetPythonBridge.Utils
{
    public static class WSL_Helper
    {
        // Lock object for thread-safe initialization
        private static readonly object _initLock = new object();
        private static WSL_Distros? Distros = null;

        /// <summary>
        /// Get a list of all installed WSL distributions on the local machine.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="PlatformNotSupportedException"></exception>
        /// <exception cref="Exception"></exception>
        public static async Task<WSL_Distros> GetWSLDistros(bool refresh = false, TimeSpan? listDistrosTimeout = null, TimeSpan? wslWarmupTimeout = null)
        {
            listDistrosTimeout ??= CondaManager._options.WSLListDistrosTimeout; // use timeout from options if not provided
            wslWarmupTimeout ??= CondaManager._options.WSLWarmupTimeout; // use timeout from options if not provided

            // Return cached distros if already retrieved and refresh is not requested
            if (Distros != null && !refresh)
                return Distros;

            // Only works on Windows 10/11+
            if (!OperatingSystem.IsWindows())
            {
                Log.Logger.LogError("WSL is only available on Windows 10/11.");
                throw new PlatformNotSupportedException("WSL is only available on Windows 10/11.");
            }

            //Distros = null; // reset cached distros
            SetDistros(null); // thread-safe reset cached distros

            try
            {
                //CancellationToken cancellationToken = new CancellationToken();
                //var result = await ProcessHelper.RunProcess("wsl", "-l -v", cancellationToken, null, Encoding.Unicode); //& use Unicode encoding to handle possible non-ASCII characters
                var result = await ProcessHelper.RunProcess("wsl", "-l -v", timeout: listDistrosTimeout, encoding: Encoding.Unicode); //& use Unicode encoding to handle possible non-ASCII characters
                if (result.ExitCode == 0 && !string.IsNullOrWhiteSpace(result.Output))
                {
                    //store the distros in WSL_Distro object
                    WSL_Distros distros = new WSL_Distros();
                    var lines = result.Output.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);

                    //skip the first header line
                    foreach (var line in lines.Skip(1))
                    {
                        // Example: "* Ubuntu-20.04 Running 2"
                        var match = Regex.Match(line, @"^\*?\s*(\S+)\s+(\S+)\s+(\d+)"); // regex to match distro name
                        if (match.Success)
                        {
                            bool isDefault = line.TrimStart().StartsWith("*"); // check if line starts with "*" and if it does, it's the default distro
                            string name = match.Groups[1].Value; // if regex matched, get the distro name
                            distros.Distros.Add(new WSL_Distro(name, isDefault)); // add the distro to the list
                            // warm up the distro
                            await WarmupWSL_Distro(name, wslWarmupTimeout);
                            Log.Logger.LogDebug($"Found WSL Distro: {name}, Default: {isDefault}");
                        }
                    }

                    // Cache the retrieved distros and return
                    //Distros = distros;
                    SetDistros(distros); // thread-safe set cached distros
                    return distros;
                }
            }
            catch (PlatformNotSupportedException ex)
            {
                Log.Logger.LogError("WSL is only available on Windows 10/11. " + ex.Message);
            }
            catch (Exception ex)
            {
                Log.Logger.LogError("Failed to retrieve WSL distributions. Ensure WSL is installed and accessible. " + ex.Message);
            }

            Log.Logger.LogError("No WSL distributions found. Please install a WSL distribution.");
            throw new InvalidOperationException("No WSL distributions found. Please install a WSL distribution.");
        }

        // an object to hold a WSL distro name and if it's default
        public class WSL_Distro
        {
            public string Name { get; set; }
            public bool IsDefault { get; set; }

            public WSL_Distro(string name, bool isDefault)
            {
                Name = name;
                IsDefault = isDefault;
            }
        }

        //an object to hold the WSL distros
        public class WSL_Distros
        {
            public List<WSL_Distro> Distros { get; set; }

            public WSL_Distros()
            {
                Distros = new List<WSL_Distro>();
            }

            //return the default distro or null if none
            public WSL_Distro? GetDefaultDistro()
            {
                return Distros.FirstOrDefault(d => d.IsDefault);// return the first default distro or null if none
            }
        }

        /// <summary>
        /// warm up a WSL distro by running a simple command in it in case it's not running.
        /// this can happen when the machine was just started and the WSL distro is not running yet.
        /// </summary>
        /// <param name="wslDistro"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
       public static async Task<PythonResult> WarmupWSL_Distro(WSL_Helper.WSL_Distro wslDistro, TimeSpan? wslWarmupTimeout = null)
        {
            wslWarmupTimeout ??= CondaManager._options.WSLWarmupTimeout; // use timeout from options if not provided

            for (int i = 0; i < CondaManager._options.WSLWarmupRetries; i++)
            {
                //string escapedDistroName = FilenameHelper.BashEscape(wslDistro.Name);
                var result = await ProcessHelper.RunProcess("wsl", $"-d {wslDistro.Name} echo WSL Distro Warmed Up", timeout: wslWarmupTimeout);
                //var result = await ProcessHelper.RunProcess("wsl", $"-d {escapedDistroName} echo WSL Distro Warmed Up");

                if (result.ExitCode == 0)
                {
                    Log.Logger.LogDebug($"WSL Distro {wslDistro.Name} is warmed up and ready.");
                    return result;
                }
                else
                {
                    Log.Logger.LogWarning($"Attempt {i + 1}: Failed to warm up WSL Distro {wslDistro.Name}. Exit Code: {result.ExitCode}, Error: {result.Error}");
                }

                // wait a bit before retrying
                await Task.Delay(CondaManager._options.WSLWarmupRetryDelay);
            }

            Log.Logger.LogError($"Failed to warm up WSL Distro {wslDistro.Name} after multiple attempts.");
            throw new InvalidOperationException($"Failed to warm up WSL Distro {wslDistro.Name}.");
        }

        /// <summary>
        /// a warm up a WSL distro by name by running a simple command in it in case it's not running.
        /// used when the developer only has the distro name and not the WSL_Distro object.
        /// </summary>
        /// <param name="wslDistroName"></param>
        /// <returns></returns>
        public static async Task<PythonResult> WarmupWSL_Distro(string wslDistroName, TimeSpan? wslWarmupTimeout = null)
        {
            wslWarmupTimeout ??= CondaManager._options.WSLWarmupTimeout; // use timeout from options if not provided

            var wslDistro = new WSL_Helper.WSL_Distro(wslDistroName, false);
            return await WarmupWSL_Distro(wslDistro, wslWarmupTimeout);
        }

        public async static Task<WSL_Distro> getDefaultWSL_Distro()
        {

            // get the default WSL distro from condaManager if available
            if (CondaManager.WSL != null)
                return CondaManager.WSL;
            else
            {
                // initalize conda manager to get the default WSL distro
                await CondaManager.Initialize();
                if (CondaManager.WSL != null)
                    return CondaManager.WSL;
                else
                    throw new InvalidOperationException("No WSL distribution specified and no default WSL distribution found in CondaManager.");
            }

        }

        //thread-safe method to set Distros
        private static void SetDistros(WSL_Distros? distros)
        {
            lock (_initLock)
            {
                Distros = distros;
            }
        }

    }
}