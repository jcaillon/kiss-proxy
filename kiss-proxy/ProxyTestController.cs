using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Titanium.Web.Proxy;
using Titanium.Web.Proxy.EventArguments;
using Titanium.Web.Proxy.Helpers;
using Titanium.Web.Proxy.Http;
using Titanium.Web.Proxy.Models;

namespace kiss_proxy {

    public class ProxyTestController {

        private bool _logOn = false;

        private readonly ProxyServer _proxyServer;

        public ProxyTestController() {
            _proxyServer = new ProxyServer {
                ExceptionFunc = ExceptionFunc,
                TrustRootCertificate = true,
                ForwardToUpstreamGateway = true,
                
            };
        }

        public void StartProxy() {
            _proxyServer.BeforeRequest += OnRequest;
            _proxyServer.BeforeResponse += OnResponse;
            _proxyServer.GetCustomUpStreamHttpProxyFunc = GetCustomUpStreamProxy;
            _proxyServer.GetCustomUpStreamHttpsProxyFunc = GetCustomUpStreamProxy;


            // get preferred outbound IP address of local machine
            var localIp = IPAddress.Any;
            using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0)) {
                socket.Connect("8.8.8.8", 65530);
                IPEndPoint endPoint = socket.LocalEndPoint as IPEndPoint;
                if (endPoint != null) {
                    localIp = endPoint.Address;
                }
            }
            var explicitEndPoint = new ExplicitProxyEndPoint(localIp, 3128, true);
            //ExcludedHttpsHostNameRegex = new List<string> { "dropbox.com", "google.com" },

            // An explicit endpoint is where the client knows about the existence of a proxy so client sends request in a proxy friendly manner
            _proxyServer.AddEndPoint(explicitEndPoint);


            _proxyServer.Start();
            _proxyServer.SetAsSystemProxy(explicitEndPoint, ProxyProtocolType.AllHttp);
            
            foreach (var endPoint in _proxyServer.ProxyEndPoints)
                Console.WriteLine("Listening on '{0}' endpoint at Ip {1} and port: {2} ", endPoint.GetType().Name, endPoint.IpAddress, endPoint.Port);
        }

        public void Stop() {
            _proxyServer.BeforeRequest -= OnRequest;
            _proxyServer.BeforeResponse -= OnResponse;

            _proxyServer.Stop();
        }


        private void ExceptionFunc(Exception exception) {
            Console.WriteLine("Exception : " + exception);
        }

        /// <summary>
        /// Gets the system up stream proxy.
        /// </summary>
        /// <param name="sessionEventArgs">The <see cref="SessionEventArgs"/> instance containing the event data.</param>
        /// <returns><see cref="ExternalProxy"/> instance containing valid proxy configuration from PAC/WAPD scripts if any exists.</returns>
        private Task<ExternalProxy> GetCustomUpStreamProxy(SessionEventArgs sessionEventArgs) {
            ExternalProxy proxy;
            if (sessionEventArgs.WebSession.Request.Host.Contains("cnaf")) {
                Console.WriteLine("PROXY CNEDI : " + sessionEventArgs.WebSession.Request.Host);
                proxy = new ExternalProxy {
                    HostName = "192.168.213.137",
                    Port = 3128
                };
            } else {
                Console.WriteLine("PROXY DEFAULT : " + sessionEventArgs.WebSession.Request.Host);
                var webProxy = WebRequest.GetSystemWebProxy().GetProxy(sessionEventArgs.WebSession.Request.RequestUri);
                proxy = new ExternalProxy {
                    HostName = webProxy.Host,
                    Port = webProxy.Port
                };
            }
            return Task.FromResult(proxy);
        }

        // intecept & cancel redirect or update requests
        public async Task OnRequest(object sender, SessionEventArgs e) {

            Console.WriteLine("Active Client Connections:" + ((ProxyServer)sender).ClientConnectionCount + " <-> " + e.WebSession.Request.Url + " <-> " + e.ClientEndPoint.Address);

            if (_logOn) {
                string body = null;
                if (e.WebSession.Request.HasBody) {
                    body = await e.GetRequestBodyAsString();
                }
                var r = e.WebSession.Request.HeaderText + (body ?? "") + (e.WebSession.Request as ConnectRequest)?.ClientHelloInfo;
                Console.WriteLine(r);
            }
        }

        //Modify response
        public async Task OnResponse(object sender, SessionEventArgs e) {

            if (_logOn) {
                string body = null;
                if (e.WebSession.Response.HasBody) {
                    body = await e.GetResponseBodyAsString();
                }
                var r = e.WebSession.Response.HeaderText + (body ?? "") + (e.WebSession.Response as ConnectResponse)?.ServerHelloInfo;
                Console.WriteLine(r);
            }
        }
    }
}
