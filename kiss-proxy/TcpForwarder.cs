using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace kiss_proxy {

    public class TcpForwarder {

        public IPEndPoint Server { get; set; }
        public IPEndPoint Client { get; set; }

        public int Buffer { get; set; }
        public bool Running { get; set; }

        private static TcpListener _listener;

        private CancellationTokenSource _cancellationTokenSource;

        public event EventHandler<ProxyDataEventArgs> ClientDataSentToServer;
        public event EventHandler<ProxyDataEventArgs> ServerDataSentToClient;
        public event EventHandler<ProxyByteDataEventArgs> BytesTransfered;

        /// <summary>
        /// Start the TCP relayer
        /// </summary>
        public async void Start() {
            if (Running == false) {
                _cancellationTokenSource = new CancellationTokenSource();
                // Check if the listener is null, this should be after the proxy has been stopped
                if (_listener == null) {
                    await AcceptConnections();
                }
            }
        }

        /// <summary>
        /// Accept Connections
        /// </summary>
        /// <returns></returns>
        private async Task AcceptConnections() {
            _listener = new TcpListener(Server.Address, Server.Port);
            var bufferSize = Buffer; // Get the current buffer size on start
            _listener.Start();
            Running = true;

            // If there is an exception we want to output the message to the console for debugging
            try {
                // While the Running bool is true, the listener is not null and there is no cancellation requested
                while (Running && _listener != null && !_cancellationTokenSource.Token.IsCancellationRequested) {
                    var client = await _listener.AcceptTcpClientAsync().WithWaitCancellation(_cancellationTokenSource.Token);
                    if (client != null) {
                        // Proxy the data from the client to the server until the end of stream filling the buffer.
                        await ProxyClientConnection(client, bufferSize);
                    }

                }
            } catch (Exception ex) {
                Console.WriteLine(ex.Message);
            }

            _listener.Stop();
        }

        /// <summary>
        /// Process the client with a predetermined buffer size
        /// </summary>
        /// <param name="client"></param>
        /// <param name="bufferSize"></param>
        /// <returns></returns>
        private async Task ProxyClientConnection(TcpClient client, int bufferSize) {

            // Handle this client
            // Send the server data to client and client data to server - swap essentially.
            var clientStream = client.GetStream();
            TcpClient server = new TcpClient(Client.Address.ToString(), Client.Port);
            var serverStream = server.GetStream();

            var cancellationToken = _cancellationTokenSource.Token;

            try {
                // Continually do the proxying
                new Task(() => ProxyClientDataToServer(client, serverStream, clientStream, bufferSize, cancellationToken), cancellationToken).Start();
                new Task(() => ProxyServerDataToClient(serverStream, clientStream, bufferSize, cancellationToken), cancellationToken).Start();
            } catch (Exception ex) {
                Console.WriteLine(ex.Message);
            }

        }

        /// <summary>
        /// Send and receive data between the Client and Server
        /// </summary>
        /// <param name="client"></param>
        /// <param name="serverStream"></param>
        /// <param name="clientStream"></param>
        /// <param name="bufferSize"></param>
        /// <param name="cancellationToken"></param>
        private void ProxyClientDataToServer(TcpClient client, NetworkStream serverStream, NetworkStream clientStream, int bufferSize, CancellationToken cancellationToken) {
            byte[] message = new byte[bufferSize];
            int clientBytes;
            while (!cancellationToken.IsCancellationRequested) {
                try {
                    clientBytes = clientStream.Read(message, 0, bufferSize);
                    if (BytesTransfered != null) {
                        var messageTrimed = message.Reverse().SkipWhile(x => x == 0).Reverse().ToArray();
                        BytesTransfered(this, new ProxyByteDataEventArgs(messageTrimed, "Client"));
                    }
                } catch {
                    // Socket error - exit loop.  Client will have to reconnect.
                    break;
                }
                if (clientBytes == 0) {
                    // Client disconnected.
                    break;
                }
                serverStream.Write(message, 0, clientBytes);

                if (ClientDataSentToServer != null) {
                    ClientDataSentToServer(this, new ProxyDataEventArgs(clientBytes));
                }
            }
            client.Close();
        }

        /// <summary>
        /// Send and receive data between the Server and Client
        /// </summary>
        /// <param name="serverStream"></param>
        /// <param name="clientStream"></param>
        /// <param name="bufferSize"></param>
        /// <param name="cancellationToken"></param>
        private void ProxyServerDataToClient(NetworkStream serverStream, NetworkStream clientStream, int bufferSize, CancellationToken cancellationToken) {
            byte[] message = new byte[bufferSize];
            int serverBytes;
            while (!cancellationToken.IsCancellationRequested) {
                try {
                    serverBytes = serverStream.Read(message, 0, bufferSize);
                    if (BytesTransfered != null) {
                        var messageTrimed = message.Reverse().SkipWhile(x => x == 0).Reverse().ToArray();
                        BytesTransfered(this, new ProxyByteDataEventArgs(messageTrimed, "Server"));
                    }
                    clientStream.Write(message, 0, serverBytes);
                } catch {
                    // Server socket error - exit loop.  Client will have to reconnect.
                    break;
                }
                if (serverBytes == 0) {
                    // server disconnected.
                    break;
                }
                if (ServerDataSentToClient != null) {
                    ServerDataSentToClient(this, new ProxyDataEventArgs(serverBytes));
                }
            }
        }


        /// <summary>
        /// Stop the Proxy Server
        /// </summary>
        public void Stop() {
            if (_listener != null && _cancellationTokenSource != null) {
                try {
                    Running = false;
                    _listener.Stop();
                    _cancellationTokenSource.Cancel();
                } catch (Exception ex) {
                    Console.WriteLine(ex.Message);
                }

                _cancellationTokenSource = null;

            }
        }

        public TcpForwarder(short port) {
            Server = new IPEndPoint(IPAddress.Any, port);
            Client = new IPEndPoint(IPAddress.Any, port + 1);
            Buffer = 4096;
        }

        public TcpForwarder(short port, IPAddress ipAddress) {
            Server = new IPEndPoint(ipAddress, port);
            Client = new IPEndPoint(ipAddress, port + 1);
            Buffer = 4096;
        }

        public TcpForwarder(short port, IPAddress ipAddress, int buffer) {
            Server = new IPEndPoint(ipAddress, port);
            Client = new IPEndPoint(ipAddress, port + 1);
            Buffer = buffer;
        }

        public TcpForwarder(ProxyDefinition definition) {
            Server = new IPEndPoint(definition.ServerAddress, definition.ServerPort);
            Client = new IPEndPoint(definition.ClientAddress, definition.ClientPort);
            Buffer = 4096;
        }


    }

    public class ProxyDefinition {
        public IPAddress ServerAddress { get; set; }
        public IPAddress ClientAddress { get; set; }
        public Int16 ServerPort { get; set; }
        public Int16 ClientPort { get; set; }
    }

    public class ProxyDataEventArgs : EventArgs {
        public int Bytes;

        public ProxyDataEventArgs(int bytes) {
            Bytes = bytes;
        }
    }

    public class ProxyByteDataEventArgs : EventArgs {
        public byte[] Bytes;
        public string Source { get; set; }
        public ProxyByteDataEventArgs(byte[] bytes, string source) {
            Bytes = bytes;
            Source = source;
        }
    }

    public static class AwaitExtensionMethods {
        public static async Task<T> WithWaitCancellation<T>(this Task<T> task, CancellationToken cancellationToken) {
            // The task completion source. 
            var tcs = new TaskCompletionSource<bool>();

            // Register with the cancellation token.
            using (cancellationToken.Register(s => ((TaskCompletionSource<bool>)s).TrySetResult(true), tcs))
                // If the task waited on is the cancellation token...
                if (task != await Task.WhenAny(task, tcs.Task))
                    throw new OperationCanceledException(cancellationToken);

            // Wait for one or the other to complete.
            return await task;
        }
    }

}
