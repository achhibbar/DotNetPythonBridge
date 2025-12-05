namespace DotNetPythonBridge
{
    public class PythonEnvironment
    {
        public string Name { get; }
        public string Path { get; }
        public string WSL_Distro { get; } // empty if not WSL

        public PythonEnvironment(string name, string path, string wslDistro = "")
        {
            Name = name;
            Path = path;
            WSL_Distro = wslDistro;
        }

        public override string ToString() => $"{Name} ({Path}) {(string.IsNullOrEmpty(WSL_Distro) ? "" : $"[WSL: {WSL_Distro}]")}";
    }

    public class PythonEnvironments
    {
        public List<PythonEnvironment> Environments { get; set; }

        public PythonEnvironments()
        {
            Environments = new List<PythonEnvironment>();
        }

        //return the based environment
        public PythonEnvironment? GetBaseEnvironment()
        {
            return Environments.FirstOrDefault(e => string.IsNullOrEmpty(e.WSL_Distro)); // return the first non-WSL environment or null if none
        }

    }
}