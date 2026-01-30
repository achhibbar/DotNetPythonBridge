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
        /// Run a Python script inside the given Conda environment.
        /// </summary>
        /// <param name="scriptPath"></param>
        /// <param name="env"></param>
        /// <param name="arguments"></param>
        /// <param name="cancellationToken"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        /// <exception cref="FileNotFoundException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        public static async Task<PythonResult> RunScript(string scriptPath, PythonEnvironment? env = null, string[]? arguments = null,
            CancellationToken cancellationToken = default, TimeSpan? timeout = null)
        {
            // Log the execution details, handle null env
            Log.Logger.LogDebug($"Running script: {scriptPath} with arguments: {arguments} in environment: {(env != null ? env.Name : "Base")}");

            string pythonExe = await GetPythonExecutable(env);
            string escapedScriptPath = FilenameHelper.EnsureFilepathQuoted(scriptPath);

            if (!File.Exists(scriptPath))
            {
                Log.Logger.LogError($"Python script not found: {scriptPath}");
                throw new FileNotFoundException($"Python script not found: {scriptPath}");
            }

            // quote and esape each argument in arguments
            var argsList = new List<string>();
            if (arguments != null)
            {
                foreach (var arg in arguments)
                {
                    argsList.Add(BashCommandBuilder.Escape(arg));
                }
            }
            string argumentsEscaped = string.Join(" ", argsList);

            var result = await ProcessHelper.RunProcess(pythonExe, $"{escapedScriptPath} {argumentsEscaped}", cancellationToken, timeout);

            if (result.ExitCode != 0)
            {
                Log.Logger.LogError($"Script execution failed with error: {result.Error}");
                throw new InvalidOperationException($"Script failed with error: {result.Error}");
            }

            return result;
        }

        /// <summary>
        /// Run a Python script inside the given Conda environment in WSL.
        /// </summary>
        /// <param name="scriptPath"></param>
        /// <param name="env"></param>
        /// <param name="wSL_Distro"></param>
        /// <param name="arguments"></param>
        /// <param name="cancellationToken"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        /// <exception cref="FileNotFoundException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        public static async Task<PythonResult> RunScriptWSL(string scriptPath, PythonEnvironment? env = null, WSL_Helper.WSL_Distro? wSL_Distro = null, string[]? arguments = null,
            CancellationToken cancellationToken = default, TimeSpan? timeout = null)
        {
            Log.Logger.LogDebug($"Running script: {scriptPath} with arguments: {arguments} in environment: {(env != null ? env.Name : "Base")}, WSL: {(wSL_Distro != null ? wSL_Distro.Name : "Default")}");

            if (wSL_Distro == null)
            {
                wSL_Distro = await getDefaultWSL_Distro();
                Log.Logger.LogDebug($"No WSL distribution specified. Using default WSL distribution: {wSL_Distro.Name}");
            }

            string pythonExe = await GetPythonExecutableWSL(env, wSL_Distro);

            if (!File.Exists(scriptPath)) //ensure the script path exists on Windows side
            {
                Log.Logger.LogError($"Python script not found: {scriptPath}");
                throw new FileNotFoundException($"Python script not found: {scriptPath}");
            }

            // quote and esape each argument in arguments
            var argsList = new List<string>();
            if (arguments != null)
            {
                foreach (var arg in arguments)
                {
                    argsList.Add(BashCommandBuilder.Escape(arg));
                }
            }
            string argumentsEscaped = string.Join(" ", argsList);


            // Build the inner bash command safely
            string bashCommand = BashCommandBuilder.BuildBashRunScriptCommand(pythonExe, FilenameHelper.convertWindowsPathToWSL(scriptPath), argumentsEscaped);

            // Run the command using bash -lic to ensure the environment is loaded correctly
            // ensure the script path is converted to WSL path for the command
            var result = await ProcessHelper.RunProcess("wsl", new[]
            {
                "-d", wSL_Distro.Name,
                "bash", "-lic", bashCommand
            },
            cancellationToken, timeout);

            if (result.ExitCode != 0)
            {
                Log.Logger.LogError($"Script execution failed with error: {result.Error}");
                throw new InvalidOperationException($"Script failed with error: {result.Error}");
            }

            return result;
        }

        /// <summary>
        /// Run inline Python code inside the given Conda environment.
        /// </summary>
        /// <param name="code"></param>
        /// <param name="env"></param>
        /// <param name="cancellationToken"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public static async Task<PythonResult> RunCode(string code, PythonEnvironment? env = null, CancellationToken cancellationToken = default, TimeSpan? timeout = null)
        {
            // Log the execution details, handle null env
            Log.Logger.LogDebug($"Running inline code in environment: {(env != null ? env.Name : "Base")}");

            string pythonExe = await GetPythonExecutable(env);

            // Escape quotes to avoid breaking shell
            string escapedCode = BashCommandBuilder.EscapeQuotes(code);

            var result = await ProcessHelper.RunProcess(pythonExe, new[] { "-c", escapedCode }, cancellationToken, timeout);

            if (result.ExitCode != 0)
            {
                Log.Logger.LogError($"Code execution failed with error: {result.Error}");
                throw new InvalidOperationException($"Code execution failed with error: {result.Error}");
            }

            return result;
        }

        /// <summary>
        /// Run inline Python code inside the given Conda environment in WSL.
        /// </summary>
        /// <param name="code"></param>
        /// <param name="env"></param>
        /// <param name="wSL_Distro"></param>
        /// <param name="cancellationToken"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public static async Task<PythonResult> RunCodeWSL(string code, PythonEnvironment? env = null, WSL_Helper.WSL_Distro? wSL_Distro = null,
            CancellationToken cancellationToken = default, TimeSpan? timeout = null)
        {
            Log.Logger.LogDebug($"Running inline code in environment: {(env != null ? env.Name : "Base")}, WSL: {(wSL_Distro != null ? wSL_Distro.Name : "Base")}");

            if (wSL_Distro == null)
            {
                wSL_Distro = await getDefaultWSL_Distro();
                Log.Logger.LogDebug($"No WSL distribution specified. Using default WSL distribution: {wSL_Distro.Name}");
            }

            string pythonExe = await GetPythonExecutableWSL(env, wSL_Distro);

            // Build the inner bash command safely
            string bashCommand = BashCommandBuilder.BuildBashRunInlineCodeCommand(pythonExe, code);

            // Prepend with wsl -d <distro> to run inside WSL using bash -lic
            var result = await ProcessHelper.RunProcess("wsl", new[]
            {
                "-d", wSL_Distro.Name,
                "bash", "-lic", bashCommand
            },
            cancellationToken, timeout);

            if (result.ExitCode != 0)
            {
                Log.Logger.LogError($"Code execution failed with error: {result.Error}");
                throw new InvalidOperationException($"Code execution failed with error: {result.Error}");
            }

            return result;
            
        }

        /// <summary>
        /// Resolve the correct python executable path for the Conda environment.
        /// </summary>
        /// <param name="env"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="FileNotFoundException"></exception>
        public static async Task<string> GetPythonExecutable(PythonEnvironment? env = null)
        {
            Log.Logger.LogDebug($"Resolving Python executable for environment: {(env != null ? env.Name : "Base")}");

            // if no environment is provided, use the base conda environment
            if (env == null && CondaManager.PythonEnvironments != null)
            {
                env = CondaManager.PythonEnvironments.GetBaseEnvironment();
                if (env == null)
                {
                    Log.Logger.LogError("No base Python environment found in CondaManager.");
                    throw new InvalidOperationException("No base Python environment found in CondaManager.");
                }
                Log.Logger.LogDebug($"No environment specified. Using base environment: {env.Name}");
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
                        throw new InvalidOperationException("No base Python environment found in CondaManager after initialization.");
                    }
                    Log.Logger.LogDebug($"No environment specified. Using base environment: {env.Name}");
                }
                else
                {
                    Log.Logger.LogError("No Python environment specified and no CondaManager available to determine base environment.");
                    throw new InvalidOperationException("No Python environment specified and no CondaManager available to determine base environment.");
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
        /// Resolve the correct python executable path for the Conda environment in WSL.
        /// </summary>
        /// <param name="env"></param>
        /// <param name="wSL_Distro"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="FileNotFoundException"></exception>
        public static async Task<string> GetPythonExecutableWSL(PythonEnvironment? env = null, WSL_Helper.WSL_Distro? wSL_Distro = null)
        {
            Log.Logger.LogDebug($"Resolving Python executable for environment: {(env != null ? env.Name : "Base")}, WSL: {(wSL_Distro != null ? wSL_Distro.Name : "Default")}");

            if (wSL_Distro == null)
            {
                wSL_Distro = await getDefaultWSL_Distro();
                Log.Logger.LogDebug($"No WSL distribution specified. Using default WSL distribution: {wSL_Distro.Name}");
            }

            // if no environment is provided, use the base conda environment
            if (env == null && CondaManager.PythonEnvironmentsWSL != null)
            {
                env = CondaManager.PythonEnvironmentsWSL.GetBaseEnvironment();
                if (env == null)
                {
                    Log.Logger.LogError("No base Python environment found in CondaManager for WSL.");
                    throw new InvalidOperationException("No base Python environment found in CondaManager for WSL.");
                }
                Log.Logger.LogDebug($"No environment specified. Using base environment: {env.Name}");
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
                        throw new InvalidOperationException("No base Python environment found in CondaManager for WSL after initialization.");
                    }
                    Log.Logger.LogDebug($"No environment specified. Using base environment: {env.Name}");
                }
                else
                {
                    Log.Logger.LogError("No Python environment specified and no CondaManager available to determine base environment for WSL.");
                    throw new InvalidOperationException("No Python environment specified and no CondaManager available to determine base environment for WSL.");
                }
            }

            // WSL layout: <envPath>/bin/python
            string exe = env.Path + "/bin/python";
            // exe for bash -lic must be properly escaped
            string escapedExe = BashCommandBuilder.BashEscape(exe);

            // warm up the WSL distro in case it's not running
            await WSL_Helper.WarmupWSL_Distro(wSL_Distro);

            var checkResult = await ProcessHelper.RunProcess("wsl", new[]
            {
                "-d", wSL_Distro.Name,
                "bash", "-c", $"test -f {escapedExe} && echo exists || echo missing"
            },
            timeout: CondaManager._options.WSLDistroDoesFileExistTimeout);

            if (checkResult.Output.Trim() != "exists") //file does not exist
            {
                // Log the contents of checkResult for debugging
                Log.Logger.LogError($"Python executable not found for environment {env.Name} at {exe} inside WSL distro {wSL_Distro.Name}. Check result output: {checkResult.Output}, Error: {checkResult.Error}");
                throw new FileNotFoundException($"Python executable not found for environment {env.Name} at {exe} inside WSL distro {wSL_Distro.Name}");
            }

            return exe;
        }


    }
}