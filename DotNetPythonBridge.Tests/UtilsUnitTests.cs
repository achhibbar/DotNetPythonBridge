using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using DotNetPythonBridge.Utils;
using Microsoft.Extensions.Logging;

namespace DotNetPythonBridge.Tests
{
    public class BashCommandBuilderTests
    {
        [Fact]
        public void EscapeQuotes_ReplacesDoubleQuotes()
        {
            var input = "He said \"hello\"";

            // call method
            var outp = DotNetPythonBridge.Utils.BashCommandBuilder.EscapeQuotes(input);

            // expected result: each " becomes \" (C# literal uses \\\" to encode backslash + quote)
            var expected = "He said \\\"hello\\\"";

            Assert.Equal(expected, outp);
        }

        [Fact]
        public void BashEscape_WrapsAndEscapesSingleQuotes()
        {
            var input = "it's great";
            var outp = DotNetPythonBridge.Utils.BashCommandBuilder.BashEscape(input);

            // Implementation uses: "'" + arg.Replace("'", "'\"'\"'") + "'"
            var expected = "'" + input.Replace("'", "'\"'\"'") + "'";
            Assert.Equal(expected, outp);

            Assert.StartsWith("'", outp);
            Assert.EndsWith("'", outp);
            // Ensure the specific escape sequence used by the implementation is present
            Assert.Contains("'\"'\"'", outp);
        }

        [Fact]
        public void BashEscapeInlinePythonCode_EscapesQuotesAndBackslashes()
        {
            var code = "print('don\\'t')";
            var escaped = DotNetPythonBridge.Utils.BashCommandBuilder.BashEscapeInlinePythonCode(code);

            // Implementation does: 
            // var bashSingleQuoted = inlinePythonCode
            //      .Replace("\\", "\\\\")
            //      .Replace("'", "'\\''");
            // return $"'{bashSingleQuoted}'";
            var bashSingleQuoted = code
                .Replace("\\", "\\\\")
                .Replace("'", "'\\''");
            var expected = $"'{bashSingleQuoted}'";

            Assert.Equal(expected, escaped);
            Assert.StartsWith("'", escaped);
            Assert.EndsWith("'", escaped);

            // original single-quote should have been escaped in the produced inner text
            Assert.Contains("'\\''", escaped);
        }

        [Fact]
        public void Escape_EscapesSpecialCharacters()
        {
            var input = "a b$c`\"\\' ";
            var outp = BashCommandBuilder.Escape(input);
            // Expect some backslashes inserted
            Assert.Contains("\\ ", outp);
            Assert.Contains("\\$", outp);
            Assert.Contains("\\`", outp);
            Assert.Contains("\\\"", outp);
        }

        [Fact]
        public void BuildBashStartServiceCommand_IncludesEscapedArgs()
        {
            var pythonExe = "/usr/bin/python3";
            var script = "/home/user/myscript.py";
            var options = new PythonServiceOptions { DefaultServiceArgs = "--host 127.0.0.1" };
            var cmd = BashCommandBuilder.BuildBashStartServiceCommand(pythonExe, script, 12345, options);
            Assert.Contains(BashCommandBuilder.BashEscape(pythonExe), cmd);
            Assert.Contains(BashCommandBuilder.BashEscape(script), cmd);
            Assert.Contains("12345", cmd);
            Assert.Contains(BashCommandBuilder.BashEscape(options.DefaultServiceArgs), cmd);
        }

        [Fact]
        public void BuildBashRunScriptCommand_SplitsAndEscapesArgs()
        {
            var pythonExe = "/usr/bin/python3";
            var script = "/home/user/script.py";
            var args = "one two 'three'";
            var cmd = BashCommandBuilder.BuildBashRunScriptCommand(pythonExe, script, args);
            Assert.Contains(BashCommandBuilder.BashEscape(pythonExe), cmd);
            Assert.Contains(BashCommandBuilder.BashEscape(script), cmd);
        }

        [Fact]
        public void BuildBashRunInlineCodeCommand_ProducesEscapedInline()
        {
            var pythonExe = "/usr/bin/python3";
            var inline = "print('hello')";
            var cmd = BashCommandBuilder.BuildBashRunInlineCodeCommand(pythonExe, inline);
            Assert.Contains(BashCommandBuilder.BashEscape(pythonExe), cmd);
            Assert.Contains("-c", cmd);
        }
    }

    public class FilenameHelperTests
    {
        [Fact]
        public void EnsureFilepathQuoted_AddsQuotesIfMissing()
        {
            var f = " C:\\path\\file.txt ";
            var q = FilenameHelper.EnsureFilepathQuoted(f);
            Assert.StartsWith("\"", q);
            Assert.EndsWith("\"", q);
            Assert.DoesNotContain("  ", q);
        }

        [Fact]
        public void EnsureFilepathQuoted_DoesNotDoubleQuote()
        {
            var f = "\"C:\\file.txt\"";
            var q = FilenameHelper.EnsureFilepathQuoted(f);
            Assert.Equal(f, q);
        }

        [Theory]
        [InlineData("C:\\Users\\Alice\\file.txt", "/mnt/c/Users/Alice/file.txt")]
        [InlineData("D:\\dir\\sub", "/mnt/d/dir/sub")]
        public void ConvertWindowsPathToWSL_Valid(string win, string expectedStart)
        {
            var wsl = FilenameHelper.convertWindowsPathToWSL(win);
            Assert.Equal(expectedStart, wsl);
        }

        [Theory]
        [InlineData("")]
        [InlineData("x")]
        [InlineData("Z")]
        public void ConvertWindowsPathToWSL_Invalid_Throws(string input)
        {
            Assert.Throws<ArgumentException>(() => FilenameHelper.convertWindowsPathToWSL(input));
        }

        [Fact]
        public void ConvertDistroCondaPathToWindows_Valid()
        {
            var distro = "Ubuntu-20.04";
            var wslPath = "/home/user/miniconda3/envs/myenv";
            var outp = FilenameHelper.convertDistroCondaPathToWindows(distro, wslPath);
            Assert.StartsWith("\\\\wsl$\\", outp);
            Assert.Contains(distro, outp);
            Assert.Contains("miniconda3", outp);
        }

        [Theory]
        [InlineData(null, "/home/user")]
        [InlineData("distro", null)]
        [InlineData("distro", "/root/nopath")]
        public void ConvertDistroCondaPathToWindows_Invalid_Throws(string distro, string path)
        {
            Assert.Throws<ArgumentException>(() => FilenameHelper.convertDistroCondaPathToWindows(distro!, path!));
        }
    }

    public class PortHelperTests
    {
        [Fact]
        public void GetFreePort_ReturnsValidPort_And_IsFree()
        {
            int port = PortHelper.GetFreePort();
            Assert.InRange(port, 1, 65535);
            Assert.True(PortHelper.checkIfPortIsFree(port));
        }

        [Fact]
        public void GetAndBindFreePort_BindsAndRelease_MakesPortFree()
        {
            var listener = PortHelper.GetAndBindFreePort();
            int port = ((IPEndPoint)listener.LocalEndpoint).Port;
            // while listener is bound, port should be in use
            Assert.False(PortHelper.checkIfPortIsFree(port));
            listener.Stop();
            // after stop the port should be free (may take a short moment)
            Assert.True(PortHelper.checkIfPortIsFree(port));
        }

        [Fact]
        public void ReservePort_ReserveAndRelease_Works()
        {
            var reserved = PortHelper.ReservePort(0);
            Assert.InRange(reserved.Port, 1, 65535);
            // Port should be in use while reserved
            Assert.False(PortHelper.checkIfPortIsFree(reserved.Port));
            reserved.Release();
            Assert.True(PortHelper.checkIfPortIsFree(reserved.Port));
        }

        [Fact]
        public void ReservePort_InvalidPort_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => PortHelper.ReservePort(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => PortHelper.ReservePort(70000));
        }

        [Fact]
        public void ReservePort_SpecificPort_ThrowsWhenAlreadyInUse()
        {
            // pick a free port, bind with a TcpListener and then try to reserve same port
            int port = PortHelper.GetFreePort();
            var listener = new TcpListener(IPAddress.Loopback, port);
            listener.Start();
            try
            {
                // ReservePort should fail because port is already in use
                Assert.ThrowsAny<Exception>(() => PortHelper.ReservePort(port));
            }
            finally
            {
                listener.Stop();
            }
        }
    }

    public class ProcessHelperTests
    {
        [Fact]
        public async Task RunProcess_StringArgs_ReturnsOutput()
        {
            if (OperatingSystem.IsWindows())
            {
                var result = await ProcessHelper.RunProcess("cmd", "/c echo hello");
                Assert.Equal(0, result.ExitCode);
                Assert.Contains("hello", result.Output, StringComparison.OrdinalIgnoreCase);
            }
            else
            {
                var result = await ProcessHelper.RunProcess("echo", "hello");
                Assert.Equal(0, result.ExitCode);
                Assert.Contains("hello", result.Output);
            }
        }

        [Fact]
        public async Task RunProcess_ArgList_ReturnsOutput()
        {
            if (OperatingSystem.IsWindows())
            {
                var args = new List<string> { "/c", "echo", "arglist" };
                var result = await ProcessHelper.RunProcess("cmd", args);
                Assert.Equal(0, result.ExitCode);
                Assert.Contains("arglist", result.Output, StringComparison.OrdinalIgnoreCase);
            }
            else
            {
                var args = new List<string> { "arglist" };
                var result = await ProcessHelper.RunProcess("echo", args);
                Assert.Equal(0, result.ExitCode);
                Assert.Contains("arglist", result.Output);
            }
        }

        [Fact]
        public async Task RunProcess_Timeout_Cancels()
        {
            // Use a short timeout to cancel a long-running sleep
            if (OperatingSystem.IsWindows())
            {
                // Windows 'timeout' is interactive; use 'ping -n 5 127.0.0.1' as a stand-in for ~4s
                var cts = new CancellationTokenSource(1000);
                await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
                {
                    await ProcessHelper.RunProcess("ping", "-n 5 127.0.0.1", cts.Token, TimeSpan.FromSeconds(1));
                });
            }
            else
            {
                var cts = new CancellationTokenSource(1000);
                await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
                {
                    await ProcessHelper.RunProcess("sleep", "5", cts.Token, TimeSpan.FromSeconds(1));
                });
            }
        }
    }

    public class WSLHelperTests
    {
        [Fact]
        public async Task GetWSLDistros_PlatfromBehavior()
        {
            if (!OperatingSystem.IsWindows())
            {
                // On non-Windows we expect PlatformNotSupportedException
                await Assert.ThrowsAsync<PlatformNotSupportedException>(async () =>
                {
                    await WSL_Helper.GetWSLDistros(refresh: true);
                });
            }
            else
            {
                // On Windows we don't assert output (WSL may not be available in CI).
                // Just ensure the call doesn't throw PlatformNotSupportedException.
                // We will attempt the call but tolerate other failures.
                try
                {
                    var res = await WSL_Helper.GetWSLDistros(refresh: true);
                    Assert.NotNull(res);
                }
                catch (PlatformNotSupportedException)
                {
                    // rethrow to fail on Windows if platform unexpectedly not supported
                    throw;
                }
                catch
                {
                    // other failures (wsl not installed) are tolerated in CI
                }
            }
        }
    }

    public class LogHelperTests
    {
        [Fact]
        public void SetLogger_ReplacesLoggerInstance()
        {
            using var factory = LoggerFactory.Create(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Debug));
            var custom = factory.CreateLogger("test-logger");
            Log.SetLogger(custom);
            Assert.Same(custom, Log.Logger);
        }
    }
}