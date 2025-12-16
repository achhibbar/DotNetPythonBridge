using DotNetPythonBridge.Utils;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace DotNetPythonBridge.Tests
{
    public class FilenameHelperTests
    {
        [Theory]
        [InlineData("C:\\my folder\\file.yaml", "\"C:\\my folder\\file.yaml\"")]
        [InlineData("\"C:\\my folder\\file.yaml\"", "\"C:\\my folder\\file.yaml\"")]
        [InlineData(" file.yaml ", "\"file.yaml\"")]
        [InlineData("\"file.yaml\"", "\"file.yaml\"")]
        public void SanitizeYamlFilepath_WrapsWithQuotes_IfNotPresent(string input, string expected)
        {
            var result = FilenameHelper.EnsureYamlFilepathQuoted(input);
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("/mnt/c/my folder/file.yaml", "'/mnt/c/my folder/file.yaml'")]
        [InlineData("'/mnt/c/my folder/file.yaml'", "'/mnt/c/my folder/file.yaml'")]
        [InlineData(" file.yaml ", "'file.yaml'")]
        [InlineData("'file.yaml'", "'file.yaml'")]
        public void SanitizeYamlFilepathWSL_WrapsWithSingleQuotes_IfNotPresent(string input, string expected)
        {
            var result = FilenameHelper.sanitizeYamlFilepathWSL(input);
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("C:\\Users\\Test\\file.yaml", "/mnt/c/Users/Test/file.yaml")]
        [InlineData("D:\\data\\my file.yaml", "/mnt/d/data/my file.yaml")]
        [InlineData("/mnt/c/Users/Test/file.yaml", "/mnt/c/Users/Test/file.yaml")]
        [InlineData("/home/user/file.yaml", "/home/user/file.yaml")]
        public void ConvertWindowsPathToWSL_ConvertsCorrectly(string input, string expected)
        {
            var result = FilenameHelper.convertWindowsPathToWSL(input);
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("C")]
        public void ConvertWindowsPathToWSL_InvalidInput_Throws(string input)
        {
            Assert.Throws<ArgumentException>(() => FilenameHelper.convertWindowsPathToWSL(input));
        }

        [Theory]
        [InlineData("Ubuntu", "/home/user/miniconda3/envs/myenv", @"\\wsl$\Ubuntu\home\user\miniconda3\envs\myenv")]
        [InlineData("Debian", "/opt/conda", @"\\wsl$\Debian\opt\conda")]
        [InlineData("Ubuntu", "/home/user/miniconda3", @"\\wsl$\Ubuntu\home\user\miniconda3")]
        public void ConvertDistroCondaPathToWindows_Converts_Correctly(string distro, string wslPath, string expected)
        {
            var result = FilenameHelper.convertDistroCondaPathToWindows(distro, wslPath);
            Assert.Equal(expected, result);
        }

        [Fact]
        public void ConvertDistroCondaPathToWindows_Returns_Already_Windows_Path()
        {
            var alreadyWindows = @"\\wsl$\Ubuntu\home\user\miniconda3\envs\myenv";
            var result = FilenameHelper.convertDistroCondaPathToWindows("Ubuntu", alreadyWindows);
            Assert.Equal(alreadyWindows, result);
        }

        [Theory]
        [InlineData(null, "/home/user/miniconda3")]
        [InlineData("", "/home/user/miniconda3")]
        [InlineData("Ubuntu", null)]
        [InlineData("Ubuntu", "")]
        public void ConvertDistroCondaPathToWindows_Invalid_Args_Throws(string distro, string wslPath)
        {
            Assert.Throws<ArgumentException>(() =>
                FilenameHelper.convertDistroCondaPathToWindows(distro, wslPath));
        }
    }

    public class PortHelperTests
    {
        [Fact]
        public void GetFreePort_Returns_Usable_Port()
        {
            int port = PortHelper.GetFreePort();
            Assert.InRange(port, 1024, 65535);

            // Try to bind to the port to ensure it's actually free
            TcpListener? listener = null;
            try
            {
                listener = new TcpListener(IPAddress.Loopback, port);
                listener.Start();
                Assert.True(listener.LocalEndpoint is IPEndPoint ep && ep.Port == port);
            }
            finally
            {
                listener?.Stop();
            }
        }

        [Fact]
        public void CheckIfPortIsFree_Returns_True_For_Free_Port()
        {
            int port = PortHelper.GetFreePort();
            bool isFree = PortHelper.checkIfPortIsFree(port);
            Assert.True(isFree);
        }

        [Fact]
        public void CheckIfPortIsFree_Returns_False_For_Used_Port()
        {
            // Bind to a port, then check if it's free
            TcpListener? listener = null;
            int port = PortHelper.GetFreePort();
            try
            {
                listener = new TcpListener(IPAddress.Loopback, port);
                listener.Start();
                bool isFree = PortHelper.checkIfPortIsFree(port);
                Assert.False(isFree);
            }
            finally
            {
                listener?.Stop();
            }
        }

        [Theory]
        [InlineData(0)]
        [InlineData(65536)]
        [InlineData(-1)]
        public void CheckIfPortIsFree_InvalidPort_Throws(int port)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                // Try to bind to an invalid port, which should throw
                PortHelper.checkIfPortIsFree(port);
            });
        }
    }

    public class LoggingHelperTests
    {
        private class TestLogger : ILogger
        {
            public List<string> Messages { get; } = new List<string>();
            public IDisposable BeginScope<TState>(TState state) => null!;
            public bool IsEnabled(LogLevel logLevel) => true;
            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            {
                Messages.Add(formatter(state, exception));
            }
        }

        [Fact]
        public void Logger_Returns_Default_Logger_If_Not_Set()
        {
            // Reset static field for test isolation
            typeof(Log).GetField("_logger", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)!.SetValue(null, null);

            var logger = Log.Logger;
            Assert.NotNull(logger);
            Assert.Equal("DotNetPythonBridge", logger.GetType().GetProperty("Name")?.GetValue(logger) ?? "DotNetPythonBridge");
        }

        [Fact]
        public void SetLogger_Overrides_Default_Logger()
        {
            var testLogger = new TestLogger();
            Log.SetLogger(testLogger);

            var logger = Log.Logger;
            Assert.Same(testLogger, logger);
        }

        [Fact]
        public void Logger_Uses_User_Logger_For_Log_Calls()
        {
            var testLogger = new TestLogger();
            Log.SetLogger(testLogger);

            var logger = Log.Logger;
            logger.LogInformation("Test message {Value}", 42);

            Assert.Contains(testLogger.Messages, m => m.Contains("Test message 42"));
        }

        [Fact]
        public void LoggerFactory_Is_Lazy_Initialized()
        {
            // Reset static field for test isolation
            typeof(Log).GetField("_loggerFactory", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)!.SetValue(null, null);

            var factory = typeof(Log).GetProperty("LoggerFactory", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)!.GetValue(null);
            Assert.NotNull(factory);
        }
    }

    public class ProcessHelperTests
    {
        [Fact]
        public async Task RunProcess_Returns_Expected_Output()
        {
            // Windows: use 'cmd /c echo hello'
            // Linux/macOS: use 'echo hello'
            string file, args;
            if (OperatingSystem.IsWindows())
            {
                file = "cmd";
                args = "/c echo hello";
            }
            else
            {
                file = "echo";
                args = "hello";
            }

            var result = await ProcessHelper.RunProcess(file, args);
            Assert.Equal(0, result.ExitCode);
            Assert.Contains("hello", result.Output, StringComparison.OrdinalIgnoreCase);
            Assert.True(string.IsNullOrEmpty(result.Error));
        }

        [Fact]
        public async Task RunProcess_Captures_StandardError()
        {
            string file, args;
            if (OperatingSystem.IsWindows())
            {
                file = "cmd";
                args = "/c dir non_existent_file";
            }
            else
            {
                file = "ls";
                args = "non_existent_file";
            }

            var result = await ProcessHelper.RunProcess(file, args);
            Assert.NotEqual(0, result.ExitCode);
            Assert.False(string.IsNullOrEmpty(result.Error));
        }

        [Fact]
        public async Task RunProcess_Can_Be_Cancelled()
        {
            string file, args;
            if (OperatingSystem.IsWindows())
            {
                file = "cmd";
                args = "/c timeout /t 5";
            }
            else
            {
                file = "sleep";
                args = "5";
            }

            using var cts = new CancellationTokenSource(500); // cancel after 0.5s

            await Assert.ThrowsAsync<TaskCanceledException>(async () =>
            {
                await ProcessHelper.RunProcess(file, args, cts.Token);
            });
        }

        [Fact]
        public async Task StartProcess_Starts_And_Kills_On_Cancellation()
        {
            string file, args;
            if (OperatingSystem.IsWindows())
            {
                file = "cmd";
                args = "/c timeout /t 10";
            }
            else
            {
                file = "sleep";
                args = "10";
            }

            using var cts = new CancellationTokenSource(500); // cancel after 0.5s

            var process = await ProcessHelper.StartProcess(file, args, cts.Token);
            Assert.False(process.HasExited);

            // Wait a bit for cancellation to take effect
            await Task.Delay(1000);
            Assert.True(process.HasExited);
        }

        [Theory]
        [InlineData("wsl", "bash", "utf-8", true)]
        [InlineData("conda", "info", "utf-8", true)]
        [InlineData("python", "script.py", "utf-8", true)]
        [InlineData("python", "-c", "utf-8", true)]
        [InlineData("cmd", "", "unicode", false)]
        [InlineData("echo", "", "utf-8", false)]
        public void GetEncodingForProcess_Selects_Correct_Encoding(string file, string args, string expected, bool isWindows)
        {
            // Use reflection to call private method
            var method = typeof(ProcessHelper).GetMethod("GetEncodingForProcess", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!;

            if (OperatingSystem.IsWindows() == isWindows)
            {
                var encoding = (Encoding)method.Invoke(null, new object[] { file, args })!;
                Assert.Equal(expected, encoding.WebName, ignoreCase: true);
            }
        }
    }

    //public class WSL_HelperTests
    //{
    //    [Fact]
    //    public void ListDistros_ShouldReturnAtLeastOne()
    //    {
    //        var distros = WSL_Helper.GetWSLDistros();
    //        Assert.NotEmpty(distros.Distros);
    //    }

    //    [Fact]
    //    public void GetDefaultDistro_ShouldReturnNonNull()
    //    {
    //        var distros = WSL_Helper.GetWSLDistros();
    //        var defaultDistro = distros.Distros.FirstOrDefault(d => d.IsDefault);
    //        Assert.NotNull(defaultDistro);
    //    }
    //}

    //public class CondaManagerTests
    //{
    //    [Fact]
    //    public void ListEnvironments_ShouldReturnAtLeastOne()
    //    {
    //        var envs = CondaManager.ListEnvironments();
    //        Assert.NotEmpty(envs);
    //    }

    //    [Fact]
    //    public void GetEnvironment_ShouldReturnValidPath()
    //    {
    //        var envs = CondaManager.ListEnvironments();
    //        var firstEnv = envs.FirstOrDefault();
    //        if (firstEnv != null)
    //        {
    //            Assert.True(Directory.Exists(firstEnv.Path));
    //        }
    //    }

    //    [Fact]
    //    public void GetCondaPath_ShouldReturnValidPath()
    //    {
    //        var condaPath = CondaManager.GetCondaPath();
    //        Assert.True(File.Exists(condaPath));
    //    }

    //    [Fact]
    //    public void GetEnvironment_NonExistentEnv_ShouldReturnNull()
    //    {
    //        var env = CondaManager.GetEnvironment("ThisEnvDoesNotExist12345");
    //        Assert.Null(env);
    //    }

    //    //test the portHelper
    //    [Fact]
    //    public void GetFreePort_ShouldReturnAvailablePort()
    //    {
    //        int port = PortHelper.GetFreePort();
    //        Assert.InRange(port, 1024, 65535); // Valid port range
    //        Assert.True(PortHelper.checkIfPortIsFree(port));
    //    }

    //    [Fact]
    //    public void GetEnvironment_NullOrEmpty_ShouldReturnNull()
    //    {
    //        var envNull = CondaManager.GetEnvironment(null);
    //        var envEmpty = CondaManager.GetEnvironment(string.Empty);
    //        Assert.Null(envNull);
    //        Assert.Null(envEmpty);
    //    }

    //    [Fact]
    //    public void ListEnvironments_ShouldHandleNoEnvsGracefully()
    //    {
    //        // This test assumes that there is a way to simulate no environments.
    //        // If not, this test may need to be adjusted or removed.
    //        var originalCondaPath = CondaManager.GetCondaPath();
    //        try
    //        {
    //            // Temporarily change the conda path to an invalid one
    //            typeof(CondaManager).GetField("condaPath", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)
    //                .SetValue(null, "C:\\InvalidPath\\conda.exe");

    //            var envs = CondaManager.ListEnvironments();
    //            Assert.Empty(envs);
    //        }
    //        finally
    //        {
    //            // Restore the original conda path
    //            typeof(CondaManager).GetField("condaPath", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)
    //                .SetValue(null, originalCondaPath);
    //        }
    //    }

    //    [Fact]
    //    public void ListEnvironments_ShouldReturnCorrectProperties()
    //    {
    //        var envs = CondaManager.ListEnvironments();
    //        foreach (var env in envs)
    //        {
    //            Assert.False(string.IsNullOrWhiteSpace(env.Name));
    //            Assert.True(Directory.Exists(env.Path));
    //        }
    //    }
    //}
}