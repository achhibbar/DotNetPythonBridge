using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("DotNetPythonBridge.Tests")]
[assembly: InternalsVisibleTo("DotNetPythonBridgeUI")]

namespace DotNetPythonBridge.Utils
{
    internal class BashCommandBuilder
    { 
        // Escape quotes to avoid breaking shell
        internal static string EscapeQuotes(string input)
            => input.Replace("\"", "\\\"");

        // for launching service via bash -lc '...'
        internal static string BashEscape(string arg)
            => "'" + arg.Replace("'", "'\"'\"'") + "'"; // escape single quotes for bash by closing, escaping, and reopening

        /// <summary>
        /// Escapes inline Python code so it can be safely passed to: 
        /// bash -lic "/path/to/python -c '...python code...'"
        /// Uses single quotes on the bash side
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

        internal static string Escape(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            // Escape special characters for bash
            return input.Replace("\\", "\\\\") // Escape backslashes
                        .Replace("'", "\\'") // Escape single quotes
                        .Replace("\"", "\\\"") // Escape double quotes
                        .Replace("`", "\\`") // Escape backticks
                        .Replace("$", "\\$") // Escape dollar signs
                        .Replace(" ", "\\ "); // Escape spaces
        }

            /// <summary>
            /// for launching service via bash -lc '...'
            /// </summary>
            /// <param name="pythonExe"></param>
            /// <param name="wslScriptPath"></param>
            /// <param name="port"></param>
            /// <param name="options"></param>
            /// <returns></returns>
            internal static string BuildBashStartServiceCommand(
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
                args.Add(BashEscape(options.DefaultServiceArgs));
                //args.Add(options.DefaultServiceArgs);

            return string.Join(" ", args);
        }

        // for running arbitrary python scripts with arguments via bash -lc '...'
        internal static string BuildBashRunScriptCommand(
            string pythonExe,
            string wslScriptPath,
            string pyScriptArgs)
        {
            var args = new List<string>
            {
                BashEscape(pythonExe),
                BashEscape(wslScriptPath)
            };

            // Split the arguments by spaces and escape each one
            if (!string.IsNullOrWhiteSpace(pyScriptArgs))
            {
                var scriptArgs = pyScriptArgs.Split(' ');
                foreach (var arg in scriptArgs)
                {
                    args.Add(BashEscape(arg));
                }
            }

            return string.Join(" ", args);
        }

        // for running inline python code via bash -lc '...'
        internal static string BuildBashRunInlineCodeCommand(
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

        internal static string BuildBashWhichCommand(string command)
        {
            return $"which {BashEscape(command)}";
        }

        /// <summary>
        /// for launching conda commands via bash -lc '...'
        /// </summary>
        /// <param name="condaPath"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public static string BuildBashCondaCommand(string condaPath, string args)
        {
            return $"{BashEscape(condaPath)} {args}";
        }

        /// <summary>
        /// Builds a bash command string to create a Conda environment, properly escaping arguments.
        /// </summary>
        /// <param name="condaPath">The full path to the conda or mamba executable.</param>
        /// <param name="yamlFile">The path to the YAML file.</param>
        /// <param name="envName">Optional environment name to override the YAML.</param>
        /// <returns>A bash command string suitable for use with WSL.</returns>
        public static string BuildBashCreateCondaEnvCmd(string condaPath, string yamlFile, string? envName = null)
        {
            string escapedCondaPath = BashEscape(condaPath);
            string escapedYamlFile = BashEscape(yamlFile);

            if (string.IsNullOrEmpty(envName))
            {
                return $"{escapedCondaPath} env create -f {escapedYamlFile}"; // no -n, use name from yaml
            }
            else
            {
                string escapedEnvName = BashEscape(envName);
                return $"{escapedCondaPath} env create -n {escapedEnvName} -f {escapedYamlFile}"; // -n to specify env name
            }
        }

        public static string BuildBashDeleteCondaEnvCmd(string condaPath, string envName)
        {
            string escapedCondaPath = BashEscape(condaPath);
            string escapedEnvName = BashEscape(envName);

            return $"{escapedCondaPath} env remove -n {escapedEnvName} -y"; // -y to auto-confirm
        }
    }
}
