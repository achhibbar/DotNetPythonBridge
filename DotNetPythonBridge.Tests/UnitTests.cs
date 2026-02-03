namespace DotNetPythonBridge.Tests
{
    public class DataModelTests
    {
        [Fact]
        public void PythonResult_StoresValues()
        {
            var pr = new PythonResult(42, "out", "err");
            Assert.Equal(42, pr.ExitCode);
            Assert.Equal("out", pr.Output);
            Assert.Equal("err", pr.Error);
        }

        [Fact]
        public void PythonEnvironment_ToString_IncludesNameAndPath()
        {
            var env = new PythonEnvironment("myenv", @"C:\path\to\env");
            var s = env.ToString();
            Assert.Contains("myenv", s);
            Assert.Contains(@"C:\path\to\env", s);
        }

        [Fact]
        public void PythonEnvironments_GetBaseEnvironment_ReturnsNonWSL()
        {
            var envs = new PythonEnvironments();
            envs.Environments.Add(new PythonEnvironment("wslenv", "/home/user/env", "Ubuntu-20.04"));
            envs.Environments.Add(new PythonEnvironment("base", @"C:\miniconda3"));
            var baseEnv = envs.GetBaseEnvironment();
            Assert.NotNull(baseEnv);
            Assert.Equal("base", baseEnv!.Name);
        }
    }

    public class OptionsAndFluentHelpersTests
    {
        [Fact]
        public void DotNetPythonBridgeOptions_FluentHelpers_SetValues()
        {
            var opts = new DotNetPythonBridgeOptions()
                .WithCondaPath(@"C:\conda\conda.exe")
                .WithWSLDistro("Ubuntu-20.04")
                .WithWSLCondaPath("/home/user/miniconda3/bin/conda")
                .WithCondaDetectionTimeout(TimeSpan.FromSeconds(3));

            Assert.Equal(@"C:\conda\conda.exe", opts.DefaultCondaPath);
            Assert.Equal("Ubuntu-20.04", opts.DefaultWSLDistro);
            Assert.Equal("/home/user/miniconda3/bin/conda", opts.DefaultWSLCondaPath);
            Assert.Equal(TimeSpan.FromSeconds(3), opts.CondaDetectionTimeout);
        }
    }

    public class RunnerAndServiceValidationTests
    {
        [Fact]
        public async Task PythonRunner_RunScript_Throws_FileNotFound_ForMissingScript()
        {
            string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".py");
            // Ensure file doesn't exist
            if (File.Exists(path)) File.Delete(path);

            await Assert.ThrowsAsync<FileNotFoundException>(async () =>
            {
                await PythonRunner.RunScript(path, env: null, arguments: null, cancellationToken: CancellationToken.None, timeout: TimeSpan.FromSeconds(2));
            });
        }

        [Fact]
        public async Task PythonRunner_RunScriptWSL_Throws_FileNotFound_ForMissingScript()
        {
            string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".py");
            if (File.Exists(path)) File.Delete(path);

            await Assert.ThrowsAsync<FileNotFoundException>(async () =>
            {
                await PythonRunner.RunScriptWSL(path, env: null, wSL_Distro: null, arguments: null, cancellationToken: CancellationToken.None, timeout: TimeSpan.FromSeconds(2));
            });
        }

        [Fact]
        public async Task PythonService_Start_Throws_FileNotFound_ForMissingScript()
        {
            string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".py");
            if (File.Exists(path)) File.Delete(path);

            await Assert.ThrowsAsync<FileNotFoundException>(async () =>
            {
                // Start will validate the script path before attempting to resolve python executables
                await PythonService.Start(path, env: null, options: null, cancellationToken: CancellationToken.None, timeout: TimeSpan.FromSeconds(2));
            });
        }

        [Fact]
        public async Task PythonService_StartWSL_Throws_FileNotFound_ForMissingScript()
        {
            string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".py");
            if (File.Exists(path)) File.Delete(path);

            await Assert.ThrowsAsync<FileNotFoundException>(async () =>
            {
                await PythonService.StartWSL(path, env: null, options: null, wsl: null, cancellationToken: CancellationToken.None, timeout: TimeSpan.FromSeconds(2));
            });
        }

        [Fact]
        public void PythonServiceHandle_Constructor_Throws_OnNull()
        {
            Assert.Throws<ArgumentNullException>(() => new PythonServiceHandle(null));
        }
    }
}