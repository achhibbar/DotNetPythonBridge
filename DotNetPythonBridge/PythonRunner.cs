using DotNetPythonBridge.Utils;
using System;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using static DotNetPythonBridge.Utils.WSL_Helper;

namespace DotNetPythonBridge
{
    public static class PythonRunner
    {
        /// <summary>
        /// Run a Python script inside the given environment.
        /// If wSL_Distro is provided, runs inside the specified WSL distribution.
        /// </summary>
        public static async Task<PythonResult> RunScript(string scriptPath, PythonEnvironment? env = null, string arguments = "")
        {
            // Log the execution details, handle null env
            Log.Logger.LogInformation($"Running script: {scriptPath} with arguments: {arguments} in environment: {(env != null ? env.Name : "Base")}");

            string pythonExe = await GetPythonExecutable(env);

            if (!File.Exists(scriptPath))
            {
                Log.Logger.LogError($"Python script not found: {scriptPath}");
                throw new FileNotFoundException($"Python script not found: {scriptPath}");
            }

            var result = await ProcessHelper.RunProcess(pythonExe, $"\"{scriptPath}\" {arguments}");
            if (result.ExitCode != 0)
            {
                Log.Logger.LogError($"Script execution failed with error: {result.Error}");
                throw new Exception($"Script failed with error: {result.Error}");
            }

            return result;
        }

        /// <summary>
        /// Run a Python script inside the given environment.
        /// If wSL_Distro is provided, runs inside the specified WSL distribution.
        /// </summary>
        public static async Task<PythonResult> RunScriptWSL(string scriptPath, PythonEnvironment? env = null, WSL_Helper.WSL_Distro? wSL_Distro = null, string arguments = "")
        {
            Log.Logger.LogInformation($"Running script: {scriptPath} with arguments: {arguments} in environment: {(env != null ? env.Name : "Base")}, WSL: {(wSL_Distro != null ? wSL_Distro.Name : "Default")}");

            if (wSL_Distro == null)
            {
                wSL_Distro = await getDefaultWSL_Distro();
                Log.Logger.LogInformation($"No WSL distribution specified. Using default WSL distribution: {wSL_Distro.Name}");
            }

            string pythonExe = await GetPythonExecutableWSL(env, wSL_Distro);

            if (!File.Exists(scriptPath)) //ensure the script path exists on Windows side
            {
                Log.Logger.LogError($"Python script not found: {scriptPath}");
                throw new FileNotFoundException($"Python script not found: {scriptPath}");
            }

            // Build the inner bash command safely
            string bashCommand = FilenameHelper.BuildBashCommand(pythonExe, FilenameHelper.convertWindowsPathToWSL(scriptPath), arguments);

            // Run the command using bash -lic to ensure the environment is loaded correctly
            // ensure the script path is converted to WSL path for the command
            var result = await ProcessHelper.RunProcess("wsl", $"-d {wSL_Distro.Name} bash -lic \"{bashCommand}\"");
            //var result = await ProcessHelper.RunProcess(
            //    "wsl", $"-d {wSL_Distro.Name} bash -lic \"{pythonExe} \\\"{FilenameHelper.convertWindowsPathToWSL(scriptPath)}\\\" {arguments}\"");

            if (result.ExitCode != 0)
            {
                Log.Logger.LogError($"Script execution failed with error: {result.Error}");
                throw new Exception($"Script failed with error: {result.Error}");
            }

            return result;
        }

        /// <summary>
        /// Run inline Python code inside the given environment.
        /// </summary>
        public static async Task<PythonResult> RunCode(string code, PythonEnvironment? env = null)
        {
            // Log the execution details, handle null env
            Log.Logger.LogInformation($"Running inline code in environment: {(env != null ? env.Name : "Base")}");

            string pythonExe = await GetPythonExecutable(env);

            // Escape quotes to avoid breaking shell
            string escapedCode = code.Replace("\"", "\\\"");

            var result = await ProcessHelper.RunProcess(pythonExe, $"-c \"{escapedCode}\"");
            if (result.ExitCode != 0)
            {
                Log.Logger.LogError($"Code execution failed with error: {result.Error}");
                throw new Exception($"Code execution failed with error: {result.Error}");
            }

            return result;
        }

        /// <summary>
        /// Run inline Python code inside the given environment.
        /// </summary>
        public static async Task<PythonResult> RunCodeWSL(string code, PythonEnvironment? env = null, WSL_Helper.WSL_Distro? wSL_Distro = null)
        {
            Log.Logger.LogInformation($"Running inline code in environment: {(env != null ? env.Name : "Base")}, WSL: {(wSL_Distro != null ? wSL_Distro.Name : "Base")}");

            if (wSL_Distro == null)
            {
                wSL_Distro = await getDefaultWSL_Distro();
                Log.Logger.LogInformation($"No WSL distribution specified. Using default WSL distribution: {wSL_Distro.Name}");
            }

            string pythonExe = await GetPythonExecutableWSL(env, wSL_Distro);

            // Build the inner bash command safely
            string bashCommand = FilenameHelper.BuildBashCommand(pythonExe, code);

            // Escape quotes to avoid breaking shell
            string escapedCode = code.Replace("\"", "\\\"");

            // Prepend with wsl -d <distro> to run inside WSL using bash -lic
            var result = await ProcessHelper.RunProcess("wsl", $"-d {wSL_Distro.Name} bash -lic \"{bashCommand}\"");
            //var result = await ProcessHelper.RunProcess("wsl", $"-d {wSL_Distro.Name} bash -lic \"{pythonExe} -c \\\"{escapedCode}\\\"\"");

            if (result.ExitCode != 0)
            {
                Log.Logger.LogError($"Code execution failed with error: {result.Error}");
                throw new Exception($"Code execution failed with error: {result.Error}");
            }

            return result;
            
        }

        /// <summary>
        /// Resolve the correct python executable path for the environment.
        /// </summary>
        public static async Task<string> GetPythonExecutable(PythonEnvironment? env = null)
        {
            Log.Logger.LogInformation($"Resolving Python executable for environment: {(env != null ? env.Name : "Base")}");

            // if no environment is provided, use the base conda environment
            if (env == null && CondaManager.PythonEnvironments != null)
            {
                env = CondaManager.PythonEnvironments.GetBaseEnvironment();
                if (env == null)
                {
                    Log.Logger.LogError("No base Python environment found in CondaManager.");
                    throw new Exception("No base Python environment found in CondaManager.");
                }
                Log.Logger.LogInformation($"No environment specified. Using base environment: {env.Name}");
            }
            else if (env == null)
            {
                // if no conda manager or no base environment, attempt to initialize conda manager
                await CondaManager.Initialize();
                if (CondaManager.PythonEnvironments != null)
                {
                    env = CondaManager.PythonEnvironments.GetBaseEnvironment();
                    if (env == null)
                    {
                        Log.Logger.LogError("No base Python environment found in CondaManager after initialization.");
                        throw new Exception("No base Python environment found in CondaManager after initialization.");
                    }
                    Log.Logger.LogInformation($"No environment specified. Using base environment: {env.Name}");
                }
                else
                {
                    Log.Logger.LogError("No Python environment specified and no CondaManager available to determine base environment.");
                    throw new Exception("No Python environment specified and no CondaManager available to determine base environment.");
                }
            }

            // Windows layout: <envPath>\python.exe
            // Linux/macOS layout: <envPath>/bin/python
            string exe = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? Path.Combine(env.Path, "python.exe")
                : Path.Combine(env.Path, "bin", "python");

            if (!File.Exists(exe))
            {
                Log.Logger.LogError($"Python executable not found for environment {env.Name} at {exe}");
                throw new FileNotFoundException($"Python executable not found for environment {env.Name} at {exe}");
            }

            return exe;
        }

        /// <summary>
        /// Resolve the correct python executable path for the environment.
        /// </summary>
        public static async Task<string> GetPythonExecutableWSL(PythonEnvironment? env = null, WSL_Helper.WSL_Distro? wSL_Distro = null)
        {
            Log.Logger.LogInformation($"Resolving Python executable for environment: {(env != null ? env.Name : "Base")}, WSL: {(wSL_Distro != null ? wSL_Distro.Name : "Default")}");

            if (wSL_Distro == null)
            {
                wSL_Distro = await getDefaultWSL_Distro();
                Log.Logger.LogInformation($"No WSL distribution specified. Using default WSL distribution: {wSL_Distro.Name}");
            }

            // if no environment is provided, use the base conda environment
            if (env == null && CondaManager.PythonEnvironmentsWSL != null)
            {
                env = CondaManager.PythonEnvironmentsWSL.GetBaseEnvironment();
                if (env == null)
                {
                    Log.Logger.LogError("No base Python environment found in CondaManager for WSL.");
                    throw new Exception("No base Python environment found in CondaManager for WSL.");
                }
                Log.Logger.LogInformation($"No environment specified. Using base environment: {env.Name}");
            }
            else if (env == null)
            {
                // if no conda manager or no base environment, attempt to initialize conda manager
                await CondaManager.Initialize();
                if (CondaManager.PythonEnvironmentsWSL != null)
                {
                    env = CondaManager.PythonEnvironmentsWSL.GetBaseEnvironment();
                    if (env == null)
                    {
                        Log.Logger.LogError("No base Python environment found in CondaManager for WSL after initialization.");
                        throw new Exception("No base Python environment found in CondaManager for WSL after initialization.");
                    }
                    Log.Logger.LogInformation($"No environment specified. Using base environment: {env.Name}");
                }
                else
                {
                    Log.Logger.LogError("No Python environment specified and no CondaManager available to determine base environment for WSL.");
                    throw new Exception("No Python environment specified and no CondaManager available to determine base environment for WSL.");
                }
            }

            // WSL layout: <envPath>/bin/python
            string exe = env.Path + "/bin/python";

            // warm up the WSL distro in case it's not running
            await WSL_Helper.WarmupWSL_Distro(wSL_Distro);

            // check if the exe exists inside WSL using bash -lic
            var checkResult = await ProcessHelper.RunProcess("wsl", $"-d {wSL_Distro.Name} bash -lic \"test -f {exe} && echo exists\"");

            if (checkResult.Output.Trim() != "exists")
            {
                Log.Logger.LogError($"Python executable not found for environment {env.Name} at {exe} inside WSL distro {wSL_Distro.Name}");
                throw new FileNotFoundException($"Python executable not found for environment {env.Name} at {exe} inside WSL distro {wSL_Distro.Name}");
            }

            return exe;
        }


    }
}