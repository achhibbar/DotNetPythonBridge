namespace DotNetPythonBridge
{
    /// <summary>
    /// Holds the result of executing a Python script or command.
    /// </summary>
    public class PythonResult
    {
        public int ExitCode { get; }
        public string Output { get; }
        public string Error { get; }

        public PythonResult(int exitCode, string output, string error)
        {
            ExitCode = exitCode;
            Output = output;
            Error = error;
        }
    }
}
