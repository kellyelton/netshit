using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Skylabs.NetShit
{
    public class RawShitSock
    {
        private bool _connected;
        private IPEndPoint _ipEndPoint;
        private string _hostname;
        private int _port;
        private int _lastping;
        public delegate void dOnError(object Sender, Exception e, String error);
        public delegate void dOnInput(object Sender, ShitBag bag);
        public delegate void dConnectionEvent(object Sender, ConnectionEvent e);
        public event dOnError onError;
        public event dOnInput onInput;
        public event dConnectionEvent onConnectionEvent;

        public TcpClient ShittySocket { get; set; }

        public bool Connected { get { return _connected; } set { _connected = value; } }

        public IPEndPoint IPendpoint { get { return _ipEndPoint; } set { _ipEndPoint = value; } }

        public string Hostname { get { return _hostname; } set { _hostname = value; } }

        public int Port { get { return _port; } set { _port = value; } }

        /// <summary>
        /// Connect to a server.
        /// </summary>
        /// <param name="Host">Host name of the server</param>
        /// <param name="Port">Port of the server.</param>
        /// <returns>Boolean. True if connected, false if an error.</returns>
        public Boolean Connect(string host, int port)
        {
            try
            {
                RegisterHandlers();
                try
                {
                    IPendpoint = Tools.HostToEndpoint(host, port);
                }
                catch (Exception e)
                {
                    onError.Invoke(this, e, "DNS Error.");
                    IPendpoint = null;
                }
                if (IPendpoint == null)
                {
                    return false;
                }
                ShittySocket = new TcpClient();
                ShittySocket.ReceiveTimeout = 10000;
                ShittySocket.Connect(IPendpoint);
                return GetAcceptedSocket(ShittySocket);
            }
            catch (Exception e)
            {
                onError.Invoke(this, e, "Connect method: " + e.Message);
            }
            return false;
        }

        public bool GetAcceptedSocket(TcpClient s)
        {
            try
            {
                RegisterHandlers();
                ShittySocket = s;
                IPendpoint = ShittySocket.Client.RemoteEndPoint as IPEndPoint;
                _connected = false;
                _hostname = IPendpoint.Address.ToString();
                _port = IPendpoint.Port;
                ShittySocket.ReceiveTimeout = 10000;
                _connected = true;
                onConnectionEvent.Invoke(this, new ConnectionEvent(_hostname, _port, ConnectionEvent.eConnectionEvent.eceConnect));
                StartReceiving();
                return true;
            }
            catch (Exception e)
            {
                onError.Invoke(this, e, "Connect method: " + e.Message);
            }
            return false;
        }

        public void Close()
        {
            if (ShittySocket.Connected)
                ShittySocket.Close();
            _connected = false;
            onConnectionEvent(this, new ConnectionEvent(_hostname, _port, ConnectionEvent.eConnectionEvent.eceDisconnect));
        }

        /// <summary>
        /// Send that sweet sweet data.
        /// </summary>
        /// <param name="data">Uhm...The sweet sweet data.</param>
        public bool WriteData(byte[] data)
        {
            try
            {
                ShittySocket.GetStream().Write(data, 0, data.Length);
                ShittySocket.GetStream().Flush();
                return true;
            }
            catch (Exception e)
            {
                onError.Invoke(this, e, "Error writing data.");
                return false;
            }
        }

        private static void SendCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.
                Socket client = (Socket)ar.AsyncState;

                // Complete sending the data to the remote device.
                int bytesSent = client.EndSend(ar);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        private void StartReceiving()
        {
            try
            {
                // Create the state object.
                ShitBag state = new ShitBag();
                state.workSocket = ShittySocket.Client;

                // Begin receiving the data from the remote device.
                ShittySocket.Client.BeginReceive(state.buffer, 0, ShitBag.BufferSize, 0,
                    new AsyncCallback(doInput), state);
            }
            catch (Exception e)
            {
                onError.Invoke(this, e, "Error trying to receive data.");
            }
        }

        private void doInput(IAsyncResult ar)
        {
            try
            {
                // Retrieve the state object and the client socket
                // from the asynchronous state object.
                ShitBag state = (ShitBag)ar.AsyncState;
                Socket client = state.workSocket;
                // Read data from the remote device.
                int bytesRead = client.EndReceive(ar);
                if (bytesRead > 0)
                {
                    // There might be more data, so store the data received so far.
                    state.sb.Append(Encoding.ASCII.GetString(state.buffer, 0, bytesRead));
                    //  Get the rest of the data.
                    client.BeginReceive(state.buffer, 0, ShitBag.BufferSize, 0,
                        new AsyncCallback(doInput), state);
                }
                else
                {
                    // All the data has arrived; put it in response.
                    if (state.sb.Length > 1)
                    {
                        onInput.Invoke(this, state);
                    }
                    //Start receiving again
                    StartReceiving();
                }
            }
            catch (Exception e)
            {
                onError.Invoke(this, e, "Error in doInput");
            }
        }

        private void RegisterHandlers()
        {
            onError += new dOnError(handleError);
            onInput += new dOnInput(handleInput);
            onConnectionEvent += new dConnectionEvent(handleConnectionEvent);
        }

        private void UnregisterHandlers()
        {
            onError -= handleError;
            onInput -= handleInput;
            onConnectionEvent -= handleConnectionEvent;
        }

        /// <summary>
        /// Whenever there is a critical error, it ends up here.
        /// </summary>
        /// <param name="sm">Object that had a boo boo</param>
        /// <param name="e">Exception passed</param>
        /// <param name="error">User friendly error message.</param>
        protected virtual void handleError(object sm, Exception e, String error)
        {
#if DEBUG
            System.Diagnostics.Debugger.Break();
#endif
        }

        /// <summary>
        /// Whenever data is received, you can always find it here!
        /// </summary>
        /// <param name="sm">Object that received the data.</param>
        /// <param name="data">byte array of data received.</param>
        /// <param name="size">Size of the byte array</param>
        protected virtual void handleInput(object Sender, ShitBag shit) { }

        /// <summary>
        /// Whenever this Connects or Disconnects, this is fired.
        /// </summary>
        /// <param name="Sender">Object that Connected or Disconnected.</param>
        /// <param name="e">Information about the Connection or Disconnection</param>
        /// <seealso cref="ConnectionEvent.cs"/>
        protected virtual void handleConnectionEvent(object Sender, ConnectionEvent e) { }
    }
}