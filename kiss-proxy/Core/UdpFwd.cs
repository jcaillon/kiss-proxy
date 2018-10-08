using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using kissproxy.Lib;

namespace kissproxy.Core {

    public class UdpFwd {
        
        public IPEndPoint Local { get; set; }
        public IPEndPoint Distant { get; set; }
        public bool Running { get; set; }

        private static UdpClient _udpListener;

        private CancellationTokenSource _cancellationTokenSource;

        public event EventHandler<ProxyDataEventArgs> ClientDataSentToServer;
        public event EventHandler<ProxyDataEventArgs> ServerDataSentToClient;
        public event EventHandler<ProxyByteDataEventArgs> BytesTransfered;

        /// <summary>
        /// Constructor
        /// </summary>
        public UdpFwd(UdpForwarder fwd) {
            Local = new IPEndPoint(!string.IsNullOrEmpty(fwd.LocalAddress) ? IPAddress.Parse(fwd.LocalAddress) : Utils.LocalMachineIpAddress, fwd.LocalPort);
            Distant = new IPEndPoint(IPAddress.Parse(fwd.DistantAddress), fwd.DistantPort);
        }

        public string ServerInfo => Local.Address + ":" + Local.Port;

        /// <summary>
        /// Start the TCP relayer
        /// </summary>
        public async void Start() {
            if (Running == false) {
                _cancellationTokenSource = new CancellationTokenSource();
                // Check if the listener is null, this should be after the proxy has been stopped
                if (_udpListener == null) {
                    await AcceptConnections();
                }
            }
        }
        
        /// <summary>
        /// Accept Connections
        /// </summary>
        /// <returns></returns>
        private async Task AcceptConnections() {
            
            _udpListener = new UdpClient(Local);
            
            Logger.Log(ProxyType.UdpForwarder, Local, Distant, "Starting udp forwarder server...");
            Running = true;

            // If there is an exception we want to output the message to the console for debugging
            try {
                // While the Running bool is true, the listener is not null and there is no cancellation requested
                while (Running && _udpListener != null && !_cancellationTokenSource.Token.IsCancellationRequested) {
                    var clientReq = await _udpListener.ReceiveAsync().WithWaitCancellation(_cancellationTokenSource.Token);
                    ProcessClientAsync(clientReq.RemoteEndPoint, clientReq.Buffer);
                }
            } catch(OperationCanceledException) {
            } catch (Exception ex) {
                ErrorHandler.LogErrors(ex);
            }
        }

        private async void ProcessClientAsync(IPEndPoint client, byte[] clientReqBuffer) {
            
            //Logger.Log(ProxyType.UdpForwarder, client, Local, $"New data received from a random client : {Encoding.Default.GetString(clientReqBuffer)}");
            
            Logger.Log(ProxyType.UdpForwarder, client, Local, $"FWD {Distant.Address}:{Distant.Port}");

            using (var udpServerConnection = new UdpClient()) {    
                udpServerConnection.Connect(Distant);

                //Logger.Log(ProxyType.UdpForwarder, Local, Distant, $"Sending stuff to distant : {Encoding.Default.GetString(clientReqBuffer)}");
                
                await udpServerConnection.SendAsync(clientReqBuffer, clientReqBuffer.Length);

                var distantResult = await udpServerConnection.ReceiveAsync().WithWaitCancellation(_cancellationTokenSource.Token);
                
                //Logger.Log(ProxyType.UdpForwarder, Distant, Local, $"Receiving stuff from distant : {Encoding.Default.GetString(distantResult.Buffer)}");
                    
                //Logger.Log(ProxyType.UdpForwarder, Local, client, "Sending it back to client");

                _udpListener.SendAsync(distantResult.Buffer, distantResult.Buffer.Length, client).Wait();
            }
        }

        /// <summary>
        /// Stop the Proxy Server
        /// </summary>
        public void Stop() {
            if (_udpListener != null && _cancellationTokenSource != null) {
                try {
                    Running = false;
                    Logger.Log(ProxyType.UdpForwarder, Distant, Local, "Stopping upd forwarder server...");
                    _cancellationTokenSource.Cancel();
                } catch (Exception ex) {
                    ErrorHandler.LogErrors(ex);
                }
                _cancellationTokenSource = null;
            }
        }
    }
    
}