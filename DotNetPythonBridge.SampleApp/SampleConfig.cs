namespace DotNetPythonBridge.SampleApp
{
    public static class SampleConfig
    {
        // Sample configuration values for testing DotNetPythonBridge functionality
        // 1. Copy the files TestService.py, TestScript.py, and testCondaEnvCreate.yml to a suitable location on your system
        // 2. Update these values for your environment

        // The name of an existing native conda environment to test with e.g. "base" or "myenv"
        // Set to null to use the base conda environment
        // If the env is being used to start a service, ensure that the environment has the required packages installed e.g uvicorn, fastapi, etc.
        public static string? CondaEnvName = null;

        // The name of an existing WSL conda environment to test with e.g. "base" or "myenv"
        // Set to null to use the base WSL conda environment
        // If the nv is being used to start a service, ensure that the environment has the required packages installed e.g uvicorn, fastapi, etc.
        public static string? WSLCondaEnvName = null;

        // The path to a test Python service script e.g., @"C:\Path\To\TestService.py"
        public static string TestServiceScriptPath = @"D:\DotNetPython_Files\TestService.py";

        // The path to a test Python script e.g., @"C:\Path\To\TestScript.py"
        public static string TestScriptPath = @"D:\DotNetPython_Files\TestScript.py";

        // An inline Python code snippet to test
        public static string TestInlineCode = "result = 0\nfor i in range(5):\n    result += i\nprint(f'Sum of first 5 numbers is: {result}')";

        // The arguments to pass to the test script (if any)
        public static string[] TestScriptArguments = new string[] { "First_Name", "Surname" };

        // The path to a test conda environment YAML file e.g., @"C:\Path\To\testCondaEnvCreate.yml"
        public static string CondaEnvYamlPath = @"D:\DotNetPython_Files\testCondaEnvCreate.yml";

        // If the name of the new conda environment is not specified in the YAML, this name can be used
        public static string NewCondaEnvName = "DotNetPythonBridgeTest-env";

        // The default WSL distro to use when initializing WSL-related functionality manually
        public static string DefaultWSLDistro = "Ubuntu";

        // The default native conda path for manual initialization
        public static string DefaultCondaPath = @"C:\Users\username\miniconda3\Scripts\conda.exe";

        // The default WSL conda path for manual initialization
        // Can either be the Linux path e.g., "/home/username/miniconda3/bin/conda"
        // or the Windows path to the WSL filesystem e.g., @"\\wsl$\Ubuntu\home\username\miniconda3\bin\conda.exe"
        public static string DefaultWSLCondaPath = @"/home/username/miniconda3/bin/conda";

        public static bool DefaultUseWSL = false; // Whether to use WSL by default for testing
    }
}
