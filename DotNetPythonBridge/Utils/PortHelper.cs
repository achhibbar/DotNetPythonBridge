using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("DotNetPythonBridge.Tests")]
[assembly: InternalsVisibleTo("DotNetPythonBridgeUI")]

namespace DotNetPythonBridge.Utils
{
    /// <summary>
    /// Port related helper methods that are cross platform (Windows, Linux, MacOS).
    /// </summary>
    internal static class PortHelper
    {
        /// <summary>
        /// Get a free TCP port on the local machine.
        /// </summary>
        /// <returns></returns>
        public static int GetFreePort()
        {
            Log.Logger.LogInformation("Searching for a free TCP port...");

            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            int port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            Log.Logger.LogInformation($"Found free port: {port}");
            return port;
        }

        public static bool checkIfPortIsFree(int port)
        {
            Log.Logger.LogInformation($"Checking if port {port} is free...");

            // Validate port number
            if (port < 1 || port > 65535)
            {
                Log.Logger.LogError($"Invalid port number: {port}. Must be between 1 and 65535.");
                throw new ArgumentOutOfRangeException(nameof(port), "Port number must be between 1 and 65535.");
            }

            bool isFree = true;
            TcpListener? listener = null;
            try
            {
                listener = new TcpListener(IPAddress.Loopback, port);
                listener.Start();
            }
            catch (SocketException)
            {
                Log.Logger.LogWarning($"Port {port} is already in use.");
                isFree = false;
            }
            finally
            {
                listener?.Stop();
            }
            Log.Logger.LogInformation($"Port {port} is {(isFree ? "free" : "in use")}.");
            return isFree;
        }
    }
}
