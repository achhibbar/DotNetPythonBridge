using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace DotNetPythonBridge.Utils
{
    public static class WSL_Helper
    {
        private static WSL_Distros? Distros = null;

        /// <summary>
        /// Get a list of all installed WSL distributions on the local machine.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="PlatformNotSupportedException"></exception>
        /// <exception cref="Exception"></exception>
        public static async Task<WSL_Distros> GetWSLDistros(bool refresh = false)
        {
            // Return cached distros if already retrieved and refresh is not requested
            if (Distros != null && !refresh)
                return Distros;

            // Only works on Windows 10/11+
            if (!OperatingSystem.IsWindows())
            {
                Log.Logger.LogError("WSL is only available on Windows 10/11.");
                throw new PlatformNotSupportedException("WSL is only available on Windows 10/11.");
            }

            Distros = null; // reset cached distros

            try
            {
                CancellationToken cancellationToken = new CancellationToken();
                var result = await ProcessHelper.RunProcess("wsl", "-l -v", cancellationToken, Encoding.Unicode); //& use Unicode encoding to handle possible non-ASCII characters
                if (result.ExitCode == 0 && !string.IsNullOrWhiteSpace(result.Output))
                {
                    //store the distris in WSL_Distro object
                    WSL_Distros distros = new WSL_Distros();
                    var lines = result.Output.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);

                    //skip the first header line
                    foreach (var line in lines.Skip(1))
                    {
                        // Example line for default distro: "*    Ubuntu-20.04    Running    2"
                        var parts = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length >= 4)
                        {
                            bool isDefault = parts[0] == "*";
                            string name = parts[1];
                            distros.Distros.Add(new WSL_Distro(name, isDefault));
                            // warm up the distro
                            await WarmupWSL_Distro(name);
                            Log.Logger.LogInformation($"Found WSL Distro: {name}, Default: {isDefault}");
                        }
                        else if (parts.Length == 3) // handle case for non-default distro without the "*"
                        {
                            bool isDefault = false;
                            string name = parts[0];
                            distros.Distros.Add(new WSL_Distro(name, isDefault));
                            // warm up the distro
                            await WarmupWSL_Distro(name);
                            Log.Logger.LogInformation($"Found WSL Distro: {name}, Default: {isDefault}");
                        } 
                    }

                    // Cache the retrieved distros and return
                    Distros = distros;
                    return distros;
                }
            }
            catch 
            {
                Log.Logger.LogError("Failed to retrieve WSL distributions. Ensure WSL is installed and accessible.");
            }

            Log.Logger.LogError("No WSL distributions found. Please install a WSL distribution.");
            throw new Exception("No WSL distributions found. Please install a WSL distribution.");
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
       public static async Task<PythonResult> WarmupWSL_Distro(WSL_Helper.WSL_Distro wslDistro)
        {
            for (int i = 0; i < 3; i++)
            {
                //string escapedDistroName = FilenameHelper.BashEscape(wslDistro.Name);
                var result = await ProcessHelper.RunProcess("wsl", $"-d {wslDistro.Name} echo WSL Distro Warmed Up"); //&
                //var result = await ProcessHelper.RunProcess("wsl", $"-d {escapedDistroName} echo WSL Distro Warmed Up");

                if (result.ExitCode == 0)
                {
                    Log.Logger.LogInformation($"WSL Distro {wslDistro.Name} is warmed up and ready.");
                    return result;
                }
                else
                {
                    Log.Logger.LogWarning($"Attempt {i + 1}: Failed to warm up WSL Distro {wslDistro.Name}. Exit Code: {result.ExitCode}, Error: {result.Error}");
                }

                // wait a bit before retrying
                await Task.Delay(1000);
            }

            Log.Logger.LogError($"Failed to warm up WSL Distro {wslDistro.Name} after multiple attempts.");
            throw new Exception($"Failed to warm up WSL Distro {wslDistro.Name}.");
        }

        /// <summary>
        /// a warm up a WSL distro by name by running a simple command in it in case it's not running.
        /// used when the developer only has the distro name and not the WSL_Distro object.
        /// </summary>
        /// <param name="wslDistroName"></param>
        /// <returns></returns>
        public static async Task<PythonResult> WarmupWSL_Distro(string wslDistroName)
        {
            var wslDistro = new WSL_Helper.WSL_Distro(wslDistroName, false);
            return await WarmupWSL_Distro(wslDistro);
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
                    throw new Exception("No WSL distribution specified and no default WSL distribution found in CondaManager.");
            }

        }

    }
}