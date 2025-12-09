using DotNetPythonBridge.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;

namespace DotNetPythonBridge
{
    public static class CondaManager
    {
        private static string? _condaPath;
        private static string? _WSL_condaPath;
        private static string? _WSL_distroName;
        private static bool _isInitialized = false;

        /// <summary>
        /// The WSL Distro being used, or null if not using WSL.
        /// </summary>
        public static WSL_Helper.WSL_Distro? WSL
        {
            get
            {
                if (string.IsNullOrEmpty(_WSL_distroName))
                {
                    return null;
                }
                return new WSL_Helper.WSL_Distro(_WSL_distroName, false);
            }
        }

        public static string? CondaPath => _condaPath; // full path to conda or mamba executable
        public static string? WSL_CondaPath => _WSL_condaPath; // full path to conda or mamba executable in WSL

        public static PythonEnvironments? PythonEnvironments { get; private set; } = null;
        public static PythonEnvironments? PythonEnvironmentsWSL { get; private set; } = null;

        /// <summary>
        /// Initialize CondaManager with optional paths.
        /// </summary>
        public static async Task Initialize(DotNetPythonBridgeOptions? options = null, bool reinitialize = false)
        { 
            if (_isInitialized && !reinitialize) // already initialized and no reinit requested
            {
                return; // already initialized
            }

            if (reinitialize) // reinit requested, clear existing paths
            {
                Log.Logger.LogInformation("Reinitializing CondaManager, clearing existing paths.");
                _condaPath = null;
                _WSL_condaPath = null;
                _WSL_distroName = null;
                _isInitialized = false;
            }

            if (options != null)// options is provided, use it to initialize paths
            {
                Log.Logger.LogInformation($"Initializing CondaManager with condaPath: {options.DefaultCondaPath}, " +
                    $"wslDistroName: {options.DefaultWSLDistro}, wslPath: {options.DefaultWSLCondaPath}");

                if (string.IsNullOrEmpty(options.DefaultCondaPath))// if no conda path provided, attempt to auto-detect
                {
                    _condaPath = await GetCondaOrMambaPath();

                    // get all the conda/mamba envs, if reinitialize is true, force refresh
                    await ListEnvironments(refresh: reinitialize);
                }
                else // if a conda path is provided, validate it
                {
                    if (!File.Exists(options.DefaultCondaPath)) // if the provided path does not exist, throw error
                    {
                        Log.Logger.LogError($"Conda not found at {options.DefaultCondaPath}");
                        throw new FileNotFoundException($"Conda not found at {options.DefaultCondaPath}");
                    }
                    else // if the provided path is valid, use it
                    {
                        _condaPath = options.DefaultCondaPath;
                        // get all the conda/mamba envs, if reinitialize is true, force refresh
                        await ListEnvironments(refresh: reinitialize);
                    }
                }


                if (options.DefaultWSLCondaPath != null && options.DefaultWSLDistro != null) // if both wsl conda path and distro are provided, use them
                {
                    // convert the wslPath to windows path and check if it exists
                    if (!File.Exists(FilenameHelper.convertDistroCondaPathToWindows(options.DefaultWSLDistro, options.DefaultWSLCondaPath)))
                    {
                        Log.Logger.LogError($"Conda not found at {options.DefaultWSLCondaPath} in WSL.");
                        throw new FileNotFoundException($"Conda not found at {options.DefaultWSLCondaPath} in WSL.");
                    }

                    // warm up the WSL distro before setting the path
                    if (options.DefaultWSLDistro != null)
                    {
                        var rslt = await WSL_Helper.WarmupWSL_Distro(options.DefaultWSLDistro);
                        if (rslt.ExitCode != 0)
                        {
                            Log.Logger.LogError($"Failed to warm up WSL Distro {options.DefaultWSLDistro}: {rslt.Error}");
                            throw new Exception($"Failed to warm up WSL Distro {options.DefaultWSLDistro}: {rslt.Error}");
                        }

                        // confirm the conda path exists in the warmed up distro
                        var whichResult =
                            await ProcessHelper.RunProcess("wsl", $"-d {options.DefaultWSLDistro} bash -lic \"which {FilenameHelper.convertWindowsPathToWSL(options.DefaultWSLCondaPath)}\"");
                        if (whichResult.ExitCode != 0 || string.IsNullOrWhiteSpace(whichResult.Output))
                        {
                            Log.Logger.LogError($"Conda not found at {options.DefaultWSLCondaPath} in WSL Distro {options.DefaultWSLDistro}.");
                            throw new FileNotFoundException($"Conda not found at {options.DefaultWSLCondaPath} in WSL Distro {options.DefaultWSLDistro}.");
                        }
                    }

                    _WSL_condaPath = options.DefaultWSLCondaPath;
                    _WSL_distroName = options.DefaultWSLDistro;

                    // get all the conda/mamba envs in WSL, if reinitialize is true, force refresh
                    await ListEnvironmentsWSL(WSL, refresh: reinitialize);
                }
                else if (options.DefaultWSLDistro != null) // if only wsl distro is provided, attempt to auto-detect conda/mamba path in that distro
                {
                    _WSL_distroName = options.DefaultWSLDistro;

                    try
                    {
                        _WSL_condaPath = await GetCondaOrMambaPathWSL(new WSL_Helper.WSL_Distro(_WSL_distroName, false));
                        // get all the conda/mamba envs in WSL, if reinitialize is true, force refresh
                        await ListEnvironmentsWSL(WSL, refresh: reinitialize);
                    }
                    catch (Exception ex)
                    {
                        Log.Logger.LogWarning($"Failed to auto-detect conda/mamba in WSL Distro {_WSL_distroName}: {ex.Message}");
                        _WSL_condaPath = null; // leave it null, user can set it later
                    }
                }
                else // if neither wsl distro nor wsl conda path is provided, do nothing and leave them null
                {

                }

                _isInitialized = true;
            }
            else // if no options provided, attempt to auto-detect conda/mamba path. Lazy initialization
            {
                Log.Logger.LogInformation("Initializing CondaManager without options, will attempt to auto-detect conda/mamba path on first use.");

                _condaPath = await GetCondaOrMambaPath();
                
                // get all the conda/mamba envs, if reinitialize is true, force refresh
                await ListEnvironments(refresh: reinitialize);
                var dstrs = await WSL_Helper.GetWSLDistros(refresh: reinitialize); // ensure WSL is available
                _WSL_distroName = dstrs.GetDefaultDistro()?.Name; // get the default WSL distro if available

                if (!string.IsNullOrEmpty(_WSL_distroName))
                {
                    try
                    {
                        _WSL_condaPath = await GetCondaOrMambaPathWSL(new WSL_Helper.WSL_Distro(_WSL_distroName, false));
                        // get all the conda/mamba envs in WSL, if reinitialize is true, force refresh
                        await ListEnvironmentsWSL(WSL, refresh: reinitialize);
                    }
                    catch (Exception ex)
                    {
                        Log.Logger.LogWarning($"Failed to auto-detect conda/mamba in WSL Distro {_WSL_distroName}: {ex.Message}");
                        _WSL_condaPath = null; // leave it null, user can set it later
                    }
                }
                else
                {
                    Log.Logger.LogInformation("No default WSL distro found, skipping WSL conda/mamba auto-detection.");
                }

                _isInitialized = true;
            }
            
        }

        public static void Reset()
        {
            Log.Logger.LogInformation("Resetting CondaManager...");

            _condaPath = null;
            _WSL_condaPath = null;
            _WSL_distroName = null;
            PythonEnvironments = null;
            PythonEnvironmentsWSL = null;
            _isInitialized = false;
        }

        public static async Task<string> GetCondaOrMambaPath()
        {
            Log.Logger.LogInformation("Getting Conda or Mamba path...");

            if (!string.IsNullOrEmpty(_condaPath))
            {
                Log.Logger.LogInformation($"Using initialized conda path: {_condaPath}");
                return _condaPath;
            }

            bool isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

            string[] executables = isWindows
                ? new[] { "conda.exe", "mamba.exe" }
                : new[] { "conda", "mamba" };

            // 1. Try PATH
            foreach (var exe in executables)
            {
                try
                {
                    var whichCmd = isWindows ? "where" : "which";
                    var result = await ProcessHelper.RunProcess(whichCmd, exe);
                    if (result.ExitCode == 0 && !string.IsNullOrWhiteSpace(result.Output))
                    {
                        var candidate = result.Output.Trim().Split('\n')[0].Trim();
                        if (File.Exists(candidate))
                        {
                            _condaPath = candidate;
                            Log.Logger.LogInformation($"Found conda/mamba at: {_condaPath}");
                            return _condaPath;
                        }
                    }
                }
                catch 
                { 
                    Log.Logger.LogWarning($"Failed to find {exe}.");
                }
            }

            // 2. Try common install locations
            string user = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

            string[] windowsCandidates = {
            Path.Combine(user, "miniconda3", "Scripts", "conda.exe"),
            Path.Combine(user, "anaconda3", "Scripts", "conda.exe"),
            Path.Combine(localAppData, "miniconda3", "Scripts", "conda.exe"),
            Path.Combine(localAppData, "anaconda3", "Scripts", "conda.exe"),
            Path.Combine(user, "miniconda3", "Scripts", "mamba.exe"),
            Path.Combine(user, "anaconda3", "Scripts", "mamba.exe"),
            Path.Combine(localAppData, "miniconda3", "Scripts", "mamba.exe"),
            Path.Combine(localAppData, "anaconda3", "Scripts", "mamba.exe")
        };

            string[] linuxMacCandidates = {
            Path.Combine(user, "miniconda3", "bin", "conda"),
            Path.Combine(user, "anaconda3", "bin", "conda"),
            "/opt/miniconda3/bin/conda",
            "/opt/anaconda3/bin/conda",
            "/usr/local/bin/conda",
            Path.Combine(user, "miniconda3", "bin", "mamba"),
            Path.Combine(user, "anaconda3", "bin", "mamba"),
            "/opt/miniconda3/bin/mamba",
            "/opt/anaconda3/bin/mamba",
            "/usr/local/bin/mamba"
        };

            var candidates = isWindows ? windowsCandidates : linuxMacCandidates;

            foreach (var candidate in candidates)
            {
                if (File.Exists(candidate))
                {
                    _condaPath = candidate;
                    Log.Logger.LogInformation($"Found conda/mamba at: {_condaPath}");
                    return _condaPath;
                }
            }

            Log.Logger.LogError("Unable to locate conda or mamba.");
            throw new FileNotFoundException("Unable to locate conda or mamba. Please call CondaManager.Initialize(path) with the full path.");
        }

        public static async Task<string> GetCondaOrMambaPathWSL(WSL_Helper.WSL_Distro? wSL_Distro = null)
        {
            Log.Logger.LogInformation($"Getting Conda or Mamba path for WSL Distro: {(wSL_Distro != null ? wSL_Distro.Name : "None")}");

            if (wSL_Distro != null) // WSL mode with a specific distro
            {
                // if wSL_Distro matches the initialized one and _WSL_condaPath is set, return it
                if (wSL_Distro.Name == _WSL_distroName && !string.IsNullOrEmpty(_WSL_condaPath))
                {
                    Log.Logger.LogInformation($"Using initialized WSL conda path: {_WSL_condaPath}");
                    return _WSL_condaPath;
                }
                // if _WSL_condaPath is set but wSL_Distro is different than initialized, warn and try to find conda/mamba in the requested distro
                if (!string.IsNullOrEmpty(_WSL_distroName) && wSL_Distro.Name != _WSL_distroName)
                {
                    Log.Logger.LogWarning($"Warning: Initialized WSL conda path is for distro " +
                        $"'{_WSL_distroName}', but requested distro is '{wSL_Distro.Name}'. Attempting to find conda/mamba in the requested distro.");
                }


                // since _WSL_condaPath is not set, this is the first time we are trying to find it in WSL
                // so this distro may need to be warmed up first
                var rslt = await WSL_Helper.WarmupWSL_Distro(wSL_Distro);
                if (rslt.ExitCode != 0)
                {
                    Log.Logger.LogError($"Failed to warm up WSL Distro {wSL_Distro.Name}: {rslt.Error}");
                    throw new InvalidOperationException($"Failed to warm up WSL Distro {wSL_Distro.Name}: {rslt.Error}");
                }

                // WSL mode
                string[] executables = { "conda", "mamba" };

                foreach (var exe in executables)
                {
                    try
                    {
                        string args = string.IsNullOrEmpty(wSL_Distro.Name)
                            ? $"bash -lic \"which {exe}\""
                            : $"-d {wSL_Distro.Name} bash -lic \"which {exe}\""; // if distro is specified, use -d to run in that distro, otherwise run in default distro

                        var result = await ProcessHelper.RunProcess("wsl", args);

                        // ensure the output is not the welcome message, and if so, try again after a delay for up to 2 times
                        for (int attempt = 0; attempt < 2; attempt++)
                        {
                            if (result.ExitCode == 0 && !string.IsNullOrWhiteSpace(result.Output))
                            {
                                var firstLine = result.Output.Trim().Split('\n')[0].Trim();
                                if (firstLine.Contains("Welcome to", StringComparison.OrdinalIgnoreCase) ||
                                                                       firstLine.Contains("Microsoft", StringComparison.OrdinalIgnoreCase) ||
                                                                                                          firstLine.Contains("WSL", StringComparison.OrdinalIgnoreCase))
                                {
                                    Log.Logger.LogWarning($"Received WSL welcome message instead of conda/mamba path. Retrying...");
                                    await Task.Delay(1000); // wait for 1 second before retrying
                                    result = await ProcessHelper.RunProcess("wsl", args);
                                }
                                else
                                {
                                    break; // valid output, break the retry loop
                                }
                            }
                            else
                            {
                                break; // command failed, break the retry loop
                            }
                        }

                        if (result.ExitCode == 0 && !string.IsNullOrWhiteSpace(result.Output))
                        {
                            var candidate = result.Output.Trim().Split('\n')[0].Trim();
                            if (!string.IsNullOrEmpty(candidate))
                            {
                                _WSL_condaPath = candidate;
                                Log.Logger.LogInformation($"Found conda/mamba in WSL at: {_WSL_condaPath}");
                                return _WSL_condaPath;
                            }
                        }
                    }
                    catch
                    {
                        Log.Logger.LogWarning($"Failed to find {exe} in WSL.");
                    }
                }

                Log.Logger.LogError("Unable to locate conda or mamba in WSL.");
                throw new FileNotFoundException("Unable to locate conda or mamba in WSL. Please call CondaManager.Initialize with the WSL conda path.");
            }
            else // wSL_Distro == null
            {
                if (!string.IsNullOrEmpty(_WSL_condaPath) && !string.IsNullOrEmpty(_WSL_distroName)) // if initialized, return it
                {
                    Log.Logger.LogInformation($"Using initialized WSL conda path: {_WSL_condaPath}");
                    return _WSL_condaPath;
                }

                if (string.IsNullOrEmpty(_WSL_distroName)) // if no initialized distro, try to get the default one
                {
                    // try to get the default WSL distro
                    _WSL_distroName = (await WSL_Helper.GetWSLDistros()).GetDefaultDistro()?.Name;

                    if (string.IsNullOrEmpty(_WSL_distroName))
                    {
                        Log.Logger.LogError("WSL Distro not specified and no default WSL Distro initialized.");
                        throw new InvalidOperationException("WSL Distro not specified and no default WSL Distro initialized.");
                    }

                    return await GetCondaOrMambaPathWSL(new WSL_Helper.WSL_Distro(_WSL_distroName, false));
                }
                else // if no initialized distro and default wsl distro cannot be found, throw error
                {
                    Log.Logger.LogError("WSL Distro not specified and no default WSL Distro initialized.");
                    throw new InvalidOperationException("WSL Distro not specified and no default WSL Distro initialized.");
                }
            }
        }

        /// <summary>
        /// List all conda environments (parsed from conda info --json).
        /// </summary>
        public static async Task<IEnumerable<PythonEnvironment>> ListEnvironments(bool refresh = false)
        {
            Log.Logger.LogInformation(CondaPath != null ? $"Listing conda environments using {CondaPath}" : "Listing conda environments...");

            if (PythonEnvironments != null && refresh == false) // if already listed and no specific distro requested, return cached list
            {
                return PythonEnvironments.Environments;
            }

            var result = await ProcessHelper.RunProcess(await GetCondaOrMambaPath(), "info --json");

            if (result.ExitCode != 0)
            {
                Log.Logger.LogError($"Failed to list environments: {result.Error}");
                throw new Exception($"Failed to list environments: {result.Error}");
            }

            using var doc = JsonDocument.Parse(result.Output);
            var envs = new List<PythonEnvironment>();

            // get the root environment path for the base env
            if (doc.RootElement.TryGetProperty("default_prefix", out var rootEnvPath))
            {
                string rootPath = rootEnvPath.GetString() ?? "";
                //string rootName = Path.GetFileName(rootPath);
                envs.Add(new PythonEnvironment("base", rootPath));
                Log.Logger.LogInformation($"Found root environment: base at {rootPath}");
            }


            if (doc.RootElement.TryGetProperty("envs", out var arr))
            {
                foreach (var envPath in arr.EnumerateArray())
                {
                    // skip the root env if it's already added
                    if (envPath.GetString() == rootEnvPath.GetString())
                        continue;

                    string path = envPath.GetString() ?? "";
                    string name = Path.GetFileName(path);
                    envs.Add(new PythonEnvironment(name, path));
                    Log.Logger.LogInformation($"Found environment: {name} at {path}");
                }
            }

            PythonEnvironments = new PythonEnvironments { Environments = envs };
            return envs;
        }

        /// <summary>
        /// List all conda environments (parsed from conda info --json).
        /// </summary>
        public static async Task<IEnumerable<PythonEnvironment>> ListEnvironmentsWSL(WSL_Helper.WSL_Distro? wslDistro = null, bool refresh = false)
        {
            Log.Logger.LogInformation($"Listing conda environments for WSL Distro: {(wslDistro != null ? wslDistro.Name : "None")}");

            if (PythonEnvironmentsWSL != null && wslDistro == null && refresh == false) // if already listed and no specific distro requested, return cached list
            {
                return PythonEnvironmentsWSL.Environments;
            }
            //if already listed for the specific distro, return cached list
            if (PythonEnvironmentsWSL != null && wslDistro == WSL && refresh == false)
            {
                return PythonEnvironmentsWSL.Environments;
            }

            if (wslDistro != null) // A specific distro is provided
            {
                // wsl -d Ubuntu bash -lic "conda info --json"
                var result = await ProcessHelper.RunProcess("wsl", $"-d {wslDistro.Name} bash -lic \"{await GetCondaOrMambaPathWSL(wslDistro)} info --json\"");

                if (result.ExitCode != 0)
                {
                    Log.Logger.LogError($"Failed to list environments in WSL: {result.Error}");
                    throw new Exception($"Failed to list environments: {result.Error}");
                }

                using var doc = JsonDocument.Parse(result.Output);
                var envs = new List<PythonEnvironment>();

                // get the root environment path for the base env
                if (doc.RootElement.TryGetProperty("default_prefix", out var rootEnvPath))
                {
                    string rootPath = rootEnvPath.GetString() ?? "";
                    //string rootName = Path.GetFileName(rootPath);
                    envs.Add(new PythonEnvironment("base", rootPath));
                    Log.Logger.LogInformation($"Found root environment: base at {rootPath}");
                }

                if (doc.RootElement.TryGetProperty("envs", out var arr))
                {
                    foreach (var envPath in arr.EnumerateArray())
                    {
                        // skip the root env if it's already added
                        if (envPath.GetString() == rootEnvPath.GetString())
                            continue;

                        string path = envPath.GetString() ?? "";
                        string name = Path.GetFileName(path);
                        envs.Add(new PythonEnvironment(name, path, wslDistro.Name));
                        Log.Logger.LogInformation($"Found environment: {name} at {path} in WSL Distro {wslDistro.Name}");
                    }
                }

                PythonEnvironmentsWSL = new PythonEnvironments { Environments = envs };
                return envs;
            }
            else // no distro provided, use initialized distro if available or get default distro
            {
                if (!string.IsNullOrEmpty(_WSL_distroName)) // if initialized, use it
                {
                    return await ListEnvironmentsWSL(new WSL_Helper.WSL_Distro(_WSL_distroName, false), true);
                }

                // try to get the default WSL distro
                _WSL_distroName = (await WSL_Helper.GetWSLDistros()).GetDefaultDistro()?.Name;

                if (string.IsNullOrEmpty(_WSL_distroName))
                {
                    Log.Logger.LogError("WSL Distro not specified and no default WSL Distro initialized.");
                    throw new Exception("WSL Distro not specified and no default WSL Distro initialized.");
                }

                return await ListEnvironmentsWSL(new WSL_Helper.WSL_Distro(_WSL_distroName, false), true);
            }
        }

        /// <summary>
        /// Get a specific environment by name.
        /// Returns a PythonEnvironment object if found, which includes the path to the environment.
        /// </summary>
        public static async Task<PythonEnvironment> GetEnvironment(string? envName = null)
        {
            Log.Logger.LogInformation(envName != null ? $"Getting conda environment '{envName}'" : "Getting base conda environment...");

            //if no env name provided, return the base env
            if (string.IsNullOrEmpty(envName))
            {
                envName = "base";
            }

            foreach (var env in await ListEnvironments())
            {
                if (env.Name.Equals(envName, StringComparison.OrdinalIgnoreCase))
                {
                    Log.Logger.LogInformation($"Found environment: {env.Name} at {env.Path}");
                    return env;
                }
            }
            Log.Logger.LogError($"Environment '{envName}' not found.");
            throw new Exception($"Environment '{envName}' not found.");
        }

        /// <summary>
        /// Get a specific environment by name.
        /// Returns a PythonEnvironment object if found, which includes the path to the environment.
        /// </summary>
        public static async Task<PythonEnvironment> GetEnvironmentWSL(string? envName = null, WSL_Helper.WSL_Distro? wslDistro = null)
        {
            Log.Logger.LogInformation($"Getting conda environment '{envName}' for WSL Distro: {(wslDistro != null ? wslDistro.Name : "None")}");

            if (wslDistro != null) // specific WSL distro is provided
            {
                if (string.IsNullOrEmpty(envName)) // if no env name provided, return the base env
                {
                    envName = "base";
                }

                foreach (var env in await ListEnvironmentsWSL(wslDistro))
                {
                    if (env.Name.Equals(envName, StringComparison.OrdinalIgnoreCase))
                    {
                        Log.Logger.LogInformation($"Found environment: {env.Name} at {env.Path} in WSL Distro {wslDistro.Name}");
                        // Return a new PythonEnvironment with the WSL distro name set
                        return new PythonEnvironment(env.Name, env.Path, wslDistro.Name);
                    }
                }
                Log.Logger.LogError($"Environment '{envName}' not found in WSL Distro {wslDistro.Name}.");
                throw new Exception($"Environment '{envName}' not found.");
            }
            else // no WSL distro provided, use initialized distro if available or get default distro
            {
                if (!string.IsNullOrEmpty(_WSL_distroName)) // if initialized, use it
                {
                    return await GetEnvironmentWSL(envName, new WSL_Helper.WSL_Distro(_WSL_distroName, false));
                }

                // try to get the default WSL distro
                _WSL_distroName = (await WSL_Helper.GetWSLDistros()).GetDefaultDistro()?.Name;

                if (string.IsNullOrEmpty(_WSL_distroName))
                {
                    Log.Logger.LogError("WSL Distro not specified and no default WSL Distro initialized.");
                    throw new Exception("WSL Distro not specified and no default WSL Distro initialized.");
                }

                return await GetEnvironmentWSL(envName, new WSL_Helper.WSL_Distro(_WSL_distroName, false));
            }
        }


        /// <summary>
        /// Create a new environment from YAML file.
        /// </summary>
        public static async Task CreateEnvironment(string yamlFile, string? envName = null)
        {
            if (!File.Exists(yamlFile))// ensure the yaml file path exists
            {
                Log.Logger.LogError($"YAML file not found: {yamlFile}");
                throw new FileNotFoundException($"YAML file not found: {yamlFile}");
            }

            PythonResult result;

            // check if yaml filepath is wrapped in quotes, if not wrap it in quotes so that paths with spaces or special chars work
            string yamlFileSanitized = FilenameHelper.sanitizeYamlFilepath(yamlFile);

            if (string.IsNullOrEmpty(envName)) // use env name from YAML
            {
                result = await ProcessHelper.RunProcess(await GetCondaOrMambaPath(), $"env create -f {yamlFileSanitized}");
            }
            else // use specified env name to override name in YAML
            {
                result = await ProcessHelper.RunProcess(await GetCondaOrMambaPath(), $"env create -n {envName} -f {yamlFileSanitized}");
            }

            if (result.ExitCode != 0)
            {
                Log.Logger.LogError($"Failed to create env {envName}: {result.Error}");
                throw new Exception($"Failed to create env {envName}: {result.Error}");
            }
        }

        /// <summary>
        /// Create a new environment from YAML file.
        /// </summary>
        public static async Task CreateEnvironmentWSL(string yamlFile, string? envName = null, WSL_Helper.WSL_Distro? wslDistro = null)
        {
            if (!File.Exists(yamlFile))// ensure the yaml file path exists
            {
                Log.Logger.LogError($"YAML file not found: {yamlFile}");
                throw new FileNotFoundException($"YAML file not found: {yamlFile}");
            }

            PythonResult result;

            if (wslDistro != null)
            {
                // if the yaml file is a windows path, convert it to WSL path
                yamlFile = FilenameHelper.convertWindowsPathToWSL(yamlFile);
                // check if yaml filepath is wrapped in single quotes, if not wrap it in single quotes so that paths with spaces or special chars work
                string yamlFileSanitized = FilenameHelper.sanitizeYamlFilepathWSL(yamlFile);

                if (string.IsNullOrEmpty(envName)) // use env name from YAML
                {
                    // Use bash -lic to properly source the environment in WSL, and wrap yamlFile in single quotes to avoid issues with special chars
                    result = await ProcessHelper.RunProcess("wsl", $"-d {wslDistro.Name} bash -lic \"{await GetCondaOrMambaPathWSL(wslDistro)} env create -f {yamlFileSanitized}\"");
                }
                else // use specified env name to override name in YAML
                {
                    // Use bash -lic to properly source the environment in WSL, and wrap yamlFile in single quotes to avoid issues with special chars
                    result = await ProcessHelper.RunProcess("wsl", $"-d {wslDistro.Name} bash -lic \"{await GetCondaOrMambaPathWSL(wslDistro)} env create -n {envName} -f {yamlFileSanitized}\"");
                }

                if (result.ExitCode != 0)
                {
                    Log.Logger.LogError($"Failed to create env {envName} in WSL: {result.Error}");
                    throw new Exception($"Failed to create env {envName} in WSL: {result.Error}");
                }
            }
            else // no WSL distro provided, use initialized distro if available or get default distro
            {
                if (!string.IsNullOrEmpty(_WSL_distroName)) // if initialized, use it
                {
                    await CreateEnvironmentWSL(yamlFile, envName, new WSL_Helper.WSL_Distro(_WSL_distroName, false));
                    return;
                }

                // try to get the default WSL distro
                _WSL_distroName = (await WSL_Helper.GetWSLDistros()).GetDefaultDistro()?.Name;

                if (string.IsNullOrEmpty(_WSL_distroName))
                {
                    Log.Logger.LogError("WSL Distro not specified and no default WSL Distro initialized.");
                    throw new Exception("WSL Distro not specified and no default WSL Distro initialized.");
                }

                await CreateEnvironmentWSL(yamlFile, envName, new WSL_Helper.WSL_Distro(_WSL_distroName, false));
                return;
            }
        }

        /// <summary>
        /// Delete an environment by name.
        /// </summary>
        public static async Task DeleteEnvironment(string envName)
        {
            Log.Logger.LogInformation(envName != null ? $"Deleting conda environment '{envName}'" : "Deleting base conda environment...");

            var result = await ProcessHelper.RunProcess(await GetCondaOrMambaPath(), $"env remove -n {envName} --yes");
            if (result.ExitCode != 0)
            {
                Log.Logger.LogError($"Failed to delete env {envName}: {result.Error}");
                throw new Exception($"Failed to delete env {envName}: {result.Error}");
            }

        }

        /// <summary>
        /// Delete an environment by name.
        /// </summary>
        public static async Task DeleteEnvironmentWSL(string envName, WSL_Helper.WSL_Distro? wslDistro = null)
        {
            Log.Logger.LogInformation($"Deleting conda environment '{envName}' for WSL Distro: {(wslDistro != null ? wslDistro.Name : "None")}");

            if (wslDistro != null) // wslDistro is provided
            {
                // Use bash -lic to properly source the environment in WSL
                var result = await ProcessHelper.RunProcess(
                    "wsl",
                    $"-d {wslDistro.Name} bash -lic \"{await GetCondaOrMambaPathWSL(wslDistro)} env remove -n {envName} --yes\""
                );
                if (result.ExitCode != 0)
                {
                    Log.Logger.LogError($"Failed to delete env {envName} in WSL: {result.Error}");
                    throw new Exception($"Failed to delete env {envName} in WSL: {result.Error}");
                }
            }
            else // no wslDistro provided, use initialized distro if available or get default distro
            {
                if (!string.IsNullOrEmpty(_WSL_distroName)) // if initialized, use it
                {
                    await DeleteEnvironmentWSL(envName, new WSL_Helper.WSL_Distro(_WSL_distroName, false));
                    return;
                }

                // try to get the default WSL distro
                _WSL_distroName = (await WSL_Helper.GetWSLDistros()).GetDefaultDistro()?.Name;

                if (string.IsNullOrEmpty(_WSL_distroName))
                {
                    Log.Logger.LogError("WSL Distro not specified and no default WSL Distro initialized.");
                    throw new Exception("WSL Distro not specified and no default WSL Distro initialized.");
                }

                await DeleteEnvironmentWSL(envName, new WSL_Helper.WSL_Distro(_WSL_distroName, false));
                return;
            }

        }

    }
}
