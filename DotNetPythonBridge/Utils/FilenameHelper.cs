using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("DotNetPythonBridge.Tests")]
[assembly: InternalsVisibleTo("DotNetPythonBridgeUI")]

namespace DotNetPythonBridge.Utils
{
    internal class FilenameHelper
    {
        internal static string sanitizeYamlFilepath(string yamlFile)
        {
            // check if yaml filepath is wrapped in quotes, if not wrap it in quotes so that paths with spaces or special chars work
            yamlFile = yamlFile.Trim(); // remove leading/trailing whitespace
            if (!yamlFile.StartsWith("\"") && !yamlFile.EndsWith("\""))
            {
                yamlFile = $"\"{yamlFile}\"";
            }
            Log.Logger.LogDebug($"Sanitized YAML filepath: {yamlFile}");
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

        internal static string BashEscape(string arg)
            => "'" + arg.Replace("'", "'\"'\"'") + "'"; // escape single quotes for bash by closing, escaping, and reopening

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

    }
}