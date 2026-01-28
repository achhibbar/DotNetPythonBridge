using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("DotNetPythonBridge.Tests")]
[assembly: InternalsVisibleTo("DotNetPythonBridgeUI")]

namespace DotNetPythonBridge.Utils
{
    internal class FilenameHelper
    {
        internal static string EnsureFilepathQuoted(string yamlFile)
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

        internal static string convertWindowsPathToWSL(string windowsPath)
        {
            // Check if the path is null or empty
            if (string.IsNullOrEmpty(windowsPath) || windowsPath.Length < 3)
            {
                Log.Logger.LogError("Invalid Windows path.");
                throw new ArgumentException("Invalid Windows path.");
            }

            // Check if the path is already in WSL format
            if (windowsPath.StartsWith("/mnt/") || windowsPath.StartsWith("/"))
            {
                return windowsPath;
            }

            // Validate the drive letter
            char driveLetter = windowsPath[0];
            if (!char.IsLetter(driveLetter) || windowsPath[1] != ':')
            {
                Log.Logger.LogError("Invalid drive letter in Windows path.");
                throw new ArgumentException("Invalid drive letter in Windows path.");
            }

            string pathWithoutDrive = windowsPath.Substring(2).Replace('\\', '/');
            Log.Logger.LogDebug($"Converted Windows path '{windowsPath}' to WSL path '/mnt/{char.ToLower(driveLetter)}{pathWithoutDrive}'");
            return $"/mnt/{char.ToLower(driveLetter)}{pathWithoutDrive}";
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

            // Check if the path is already in Windows format
            if (WSL_condaPath.StartsWith("\\\\wsl$\\"))
            {
                return WSL_condaPath;
            }

            // Validate that the WSL conda path starts with a valid prefix
            if (!WSL_condaPath.StartsWith("/home/"))
            {
                Log.Logger.LogError("WSL conda path must start with '/home/'.");
                throw new ArgumentException("Invalid WSL conda path.");
            }

            string windowsPath = $"\\\\wsl$\\{WSL_DistroName}{WSL_condaPath.Replace('/', '\\')}";
            Log.Logger.LogDebug($"Converted WSL conda path '{WSL_condaPath}' in distro '{WSL_DistroName}' to Windows path '{windowsPath}'");
            return windowsPath;
        }
    }


}