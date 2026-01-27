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

        /// <summary>
        /// get a free TCP port on the local machine and bind to it, returning the TcpListener
        /// </summary>
        internal static TcpListener GetAndBindFreePort()
        {
            Log.Logger.LogInformation("Searching for and binding to a free TCP port...");

            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            int port = ((IPEndPoint)listener.LocalEndpoint).Port;
            Log.Logger.LogInformation($"Found and bound to free port: {port}");
            return listener;
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

        public class ReservedPort
        {
            public TcpListener Listener { get; }
            public int Port { get; }

            public ReservedPort(TcpListener listener, int port)
            {
                Listener = listener;
                Port = port;
            }

            public void Release()
            {
                Listener.Stop();
                Log.Logger.LogInformation($"Released reserved port: {Port}");
            }
        }

        public static ReservedPort ReservePort(int port = 0)
        {
            if (port == 0) // find and reserve a free port
            {
                var listener = GetAndBindFreePort();
                int assignedPort = ((IPEndPoint)listener.LocalEndpoint).Port;
                return new ReservedPort(listener, assignedPort);
            }
            else // reserve the specified port
            {
                // Validate port number
                if (port < 1 || port > 65535)
                {
                    Log.Logger.LogError($"Invalid port number: {port}. Must be between 1 and 65535.");
                    throw new ArgumentOutOfRangeException(nameof(port), "Port number must be between 1 and 65535.");
                }

                var listener = new TcpListener(IPAddress.Loopback, port);
                try
                {
                    listener.Start();
                    Log.Logger.LogInformation($"Reserved specified port: {port}");
                    return new ReservedPort(listener, port);
                }
                catch (SocketException)
                {
                    Log.Logger.LogError($"Port {port} is already in use and cannot be reserved.");
                    throw new PortInUseException(port);
                }
            }
        }
    }

    internal class PortInUseException : Exception
    {
        internal PortInUseException(int port)
            : base($"Port {port} is already in use and cannot be reserved.")
        {
        }
    }
}


    
