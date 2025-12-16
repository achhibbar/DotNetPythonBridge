using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("DotNetPythonBridge.Tests")]
[assembly: InternalsVisibleTo("DotNetPythonBridgeUI")]

namespace DotNetPythonBridge.Utils
{
    internal class FilenameHelper
    {
        internal static string EnsureYamlFilepathQuoted(string yamlFile)
        {
            // check if yaml filepath is wrapped in quotes, if not wrap it in quotes so that paths with spaces or special chars work
            yamlFile = yamlFile.Trim(); // remove leading/trailing whitespace
            if (!yamlFile.StartsWith("\"") && !yamlFile.EndsWith("\""))
            {
                yamlFile = $"\"{yamlFile}\"";
            }
            Log.Logger.LogDebug($"Ensured YAML filepath is quoted: {yamlFile}");
            return yamlFile;
        }

        internal static string sanitizeYamlFilepathWSL(string yamlFile)
        {
            // check if yaml filepath is wrapped in single quotes, if not wrap it in single quotes so that paths with spaces or special chars work
            yamlFile = yamlFile.Trim();
            if (!yamlFile.StartsWith("'") && !yamlFile.EndsWith("'"))
            {
                yamlFile = $"'{yamlFile}'";
            }
            Log.Logger.LogDebug($"Sanitized YAML filepath for WSL: {yamlFile}");
            return yamlFile;
        }

        internal static string convertWindowsPathToWSL(string windowsPath)
        {
            // check if there is an error in the path
            if (string.IsNullOrEmpty(windowsPath) || windowsPath.Length < 2)
            {
                Log.Logger.LogError("Invalid Windows path.");
                throw new ArgumentException("Invalid Windows path.");
            }

            //check if the the path is already in WSL format
            if (windowsPath.StartsWith("/mnt/") || windowsPath.StartsWith("/"))
                return windowsPath;

            char driveLetter = char.ToLower(windowsPath[0]);
            string pathWithoutDrive = windowsPath.Substring(2).Replace('\\', '/');
            Log.Logger.LogInformation($"Converted Windows path '{windowsPath}' to WSL path '/mnt/{driveLetter}{pathWithoutDrive}'");
            return $"/mnt/{driveLetter}{pathWithoutDrive}";
        }

        internal static string convertDistroCondaPathToWindows(string WSL_DistroName, string WSL_condaPath)
        {
            // Example: WSL_condaPath = /home/user/miniconda3/envs/myenv
            // Convert to Windows path: \\wsl$\DistroName\home\user\miniconda3\envs\myenv

            if (string.IsNullOrEmpty(WSL_DistroName) || string.IsNullOrEmpty(WSL_condaPath))
            {
                Log.Logger.LogError("Invalid WSL distro name or conda path.");
                throw new ArgumentException("Invalid WSL distro name or conda path.");
            }

            //check if the the path is already in Windows format
            if (WSL_condaPath.StartsWith("\\\\wsl$\\"))
                return WSL_condaPath;

            string windowsPath = $"\\\\wsl$\\{WSL_DistroName}{WSL_condaPath.Replace('/', '\\')}";
            Log.Logger.LogInformation($"Converted WSL conda path '{WSL_condaPath}' in distro '{WSL_DistroName}' to Windows path '{windowsPath}'");
            return windowsPath;
        }

        // Escape quotes to avoid breaking shell
        internal static string EscapeQuotes(string input)
            => input.Replace("\"", "\\\"");

        // for launching service via bash -lc '...'
        internal static string BashEscape(string arg)
            => "'" + arg.Replace("'", "'\"'\"'") + "'"; // escape single quotes for bash by closing, escaping, and reopening

        /// <summary>
        /// Escapes inline Python code so it can be safely passed to: 
        /// bash -lic "/path/to/python -c '...python code...'"
        /// Uses single quotes on the bash side (recommended and bulletproof).
        /// </summary>
        internal static string BashEscapeInlinePythonCode(string inlinePythonCode)
        {
            // Bash single-quoted string: 'don''t' → literal don't
            // So we replace every ' with '\'' (close quote, escaped quote, reopen quote)
            var bashSingleQuoted = inlinePythonCode
                .Replace("\\", "\\\\")   // optional: protect backslashes if you want them literal
                .Replace("'", "'\\''");  // the key: escape single quotes correctly

            return $"'{bashSingleQuoted}'";
        }

        /// <summary>
        /// for launching service via bash -lc '...'
        /// </summary>
        /// <param name="pythonExe"></param>
        /// <param name="wslScriptPath"></param>
        /// <param name="port"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        internal static string BuildBashCommand(
        string pythonExe,
        string wslScriptPath,
        int port,
        PythonServiceOptions options)
        {
            var args = new List<string>
            {
                BashEscape(pythonExe),
                BashEscape(wslScriptPath),
                "--port", port.ToString()
            };

            if (!string.IsNullOrWhiteSpace(options.DefaultServiceArgs))
                args.Add(options.DefaultServiceArgs);

            return string.Join(" ", args);
        }

        // for running arbitrary python scripts with arguments via bash -lc '...'
        internal static string BuildBashCommand(
        string pythonExe,
        string wslScriptPath,
        string pyScriptArgs)
        {
            var args = new List<string>
            {
                BashEscape(pythonExe),
                BashEscape(wslScriptPath),
                pyScriptArgs
            };

            return string.Join(" ", args);
        }

        // for running inline python code via bash -lc '...'
        internal static string BuildBashCommand(
            string pythonExe,
            string inlinePythonCode)
        {
               var args = new List<string>
               {
                BashEscape(pythonExe),
                "-c",
                BashEscapeInlinePythonCode(inlinePythonCode)
            };

            return string.Join(" ", args);
        }
    }

    public class WSLCommandBuilder
    {
        private readonly string _distroName;
        private readonly List<string> _wslArgs = new();
        private readonly List<string> _bashArgs = new();
        private bool _useBash = false;

        public WSLCommandBuilder(string distroName)
        {
            _distroName = distroName;
        }

        // ------------------------------------------------------------
        // WSL Layer (arguments passed directly to: wsl.exe -d <distro> ...)
        // ------------------------------------------------------------

        /// <summary>
        /// Adds a raw argument directly to WSL (no escaping).
        /// </summary>
        public WSLCommandBuilder AddWSLArg(string arg)
        {
            _wslArgs.Add(arg);
            return this;
        }

        /// <summary>
        /// Adds multiple raw WSL args.
        /// </summary>
        public WSLCommandBuilder AddWSLArgs(params string[] args)
        {
            _wslArgs.AddRange(args);
            return this;
        }

        /// <summary>
        /// Enables Bash execution using: bash -lc "<command>"
        /// </summary>
        public WSLCommandBuilder UseBash()
        {
            _useBash = true;
            return this;
        }

        // ------------------------------------------------------------
        // Bash Layer (arguments that WSL passes to bash -lc "...")
        // ------------------------------------------------------------

        /// <summary>
        /// Adds a bash argument with correct POSIX escaping.
        /// </summary>
        public WSLCommandBuilder AddBashArg(string arg)
        {
            _bashArgs.Add(BashEscape(arg));
            return this;
        }

        /// <summary>
        /// Adds multiple bash arguments safely.
        /// </summary>
        public WSLCommandBuilder AddBashArgs(params string[] args)
        {
            foreach (var a in args)
                _bashArgs.Add(BashEscape(a));

            return this;
        }

        // ------------------------------------------------------------
        // Build
        // ------------------------------------------------------------

        /// <summary>
        /// Produces the final ProcessHelper-compatible (file, args[]) pair.
        /// </summary>
        public (string file, IEnumerable<string> args) Build()
        {
            var finalArgs = new List<string>();

            finalArgs.Add("-d");
            finalArgs.Add(_distroName);

            // Raw WSL args
            finalArgs.AddRange(_wslArgs);

            if (_useBash)
            {
                finalArgs.Add("bash");
                finalArgs.Add("-lc");

                string bashCommand = string.Join(" ", _bashArgs);
                finalArgs.Add(bashCommand);
            }

            return ("wsl", finalArgs);
        }

        // ------------------------------------------------------------
        // Bash escaping utility
        // ------------------------------------------------------------

        private static string BashEscape(string arg)
        {
            // Surround with single quotes, escape existing ones correctly.
            // Example: abc'def → 'abc'"'"'def'
            return "'" + arg.Replace("'", "'\"'\"'") + "'";
        }
    }
}