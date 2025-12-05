namespace DotNetPythonBridge
{
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
