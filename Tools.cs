using System;
using System.Net;

namespace Skylabs.NetShit
{
    public static class Tools
    {
        /// <summary>
        /// Converts a string host, such as "www.google.com" or "localhost" to an IPEndPoint.
        /// </summary>
        /// <param name="hostName">Host name, such as "www.google.com" or "localhost"</param>
        /// <param name="port">Port of the server.</param>
        /// <returns>System.Net.IPEndPoint, or NULL on error.</returns>
        /// <exception cref="System.Exception">Thrown when something fails horrifically.</exception>
        public static IPEndPoint HostToEndpoint(String hostName, Int32 port)
        {
            try
            {
                IPHostEntry host = Dns.GetHostEntry(hostName);
                // Addres of the host.
                IPAddress[] addressList = host.AddressList;
                // Instantiates the endpoint and socket.
                return new IPEndPoint(addressList[addressList.Length - 1], port);
            }
            catch(Exception e) { throw (e); }
        }
    }
}