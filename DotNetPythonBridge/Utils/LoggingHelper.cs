using Microsoft.Extensions.Logging;

namespace DotNetPythonBridge.Utils
{
    public static class Log
    {
        private static ILogger? _logger;  // Static logger instance, shared across library
        private static ILoggerFactory? _loggerFactory; // Used to create logger

        // Optionally, allow users to set their own logger
        public static void SetLogger(ILogger logger) => _logger = logger;

        // Lazy-initialized logger factory
        private static ILoggerFactory LoggerFactory
        {
            get
            {
                if (_loggerFactory == null)
                {
                    _loggerFactory = Microsoft.Extensions.Logging.LoggerFactory.Create(builder =>
                    {
                        builder.AddSimpleConsole(options =>
                        {
                            options.SingleLine = true;
                            options.TimestampFormat = "[HH:mm:ss] ";
                        })
                        .SetMinimumLevel(LogLevel.Information);
                    });
                }
                return _loggerFactory;
            }
        }

        // Returns the logger — either the user-provided one, or a default one
        public static ILogger Logger => _logger ??= LoggerFactory.CreateLogger("DotNetPythonBridge");
    }
}