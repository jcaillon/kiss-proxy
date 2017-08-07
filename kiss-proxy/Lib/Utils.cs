using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace kissproxy.Lib {

    /// <summary>
    /// Utilities class
    /// </summary>
    public static class Utils {

        /// <summary>
        /// get preferred outbound IP address of local machine or by default 127.0.0.1
        /// </summary>
        public static IPAddress LocalMachineIpAddress {
            get {
                // 
                var localIp = IPAddress.Loopback;
                using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0)) {
                    socket.Connect("8.8.8.8", 65530);
                    IPEndPoint endPoint = socket.LocalEndPoint as IPEndPoint;
                    if (endPoint != null) {
                        localIp = endPoint.Address;
                    }
                }
                return localIp;
            }
        }
    }
}
