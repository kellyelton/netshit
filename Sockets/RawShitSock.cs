using System;
using System.Net;
using System.Net.Sockets;

namespace Skylabs.NetShit
{
    public class RawShitSock
    {
        private bool _connected;
        private IPEndPoint _ipEndPoint;
        private string _hostname;
        private int _port;
        private bool bCalledClose = false;
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
                catch(Exception e)
                {
#if DEBUG
                    System.Diagnostics.Debugger.Break();
#endif
                    onError.Invoke(this, e, "DNS Error.");
                    IPendpoint = null;
                }
                if(IPendpoint == null)
                {
                    return false;
                }
                ShittySocket = new TcpClient();
                ShittySocket.Connect(IPendpoint);
                return GetAcceptedSocket(ShittySocket);
            }
            catch(SocketException se)
            {
                return false;
            }
            catch(Exception e)
            {
#if DEBUG
                System.Diagnostics.Debugger.Break();
#endif
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
                _connected = true;
                onConnectionEvent.Invoke(this, new ConnectionEvent(_hostname, _port, ConnectionEvent.eConnectionEvent.eceConnect));
                StartReceiving();
                return true;
            }
            catch(Exception e)
            {
#if DEBUG
                System.Diagnostics.Debugger.Break();
#endif
                onError.Invoke(this, e, "Connect method: " + e.Message);
            }
            return false;
        }

        public void Close()
        {
            if(!bCalledClose)
            {
                bCalledClose = true;
                if(ShittySocket.Client != null)
                    if(ShittySocket.Connected)
                        try
                        {
                            ShittySocket.Close();
                        }
                        catch(SocketException se) { }
                _connected = false;
                onConnectionEvent(this, new ConnectionEvent(_hostname, _port, ConnectionEvent.eConnectionEvent.eceDisconnect));
            }
        }

        /// <summary>
        /// Send that sweet sweet data.
        /// </summary>
        /// <param name="data">Uhm...The sweet sweet data.</param>
        public bool WriteData(byte[] data)
        {
            try
            {
                ShittySocket.Client.Send(data);
                return true;
            }
            catch(SocketException se)
            {
                this.Close();
                return false;
            }
            catch(Exception e)
            {
#if DEBUG
                System.Diagnostics.Debugger.Break();
#endif
                onError.Invoke(this, e, "Error writing data.");
                return false;
            }
        }

        private void SendCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.
                Socket client = (Socket)ar.AsyncState;

                // Complete sending the data to the remote device.
                int bytesSent = client.EndSend(ar);
            }
            catch(Exception e)
            {
#if DEBUG
                System.Diagnostics.Debugger.Break();
#endif
                onError.Invoke(this, e, "Error writing data.");
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
            catch(Exception e)
            {
#if DEBUG
                System.Diagnostics.Debugger.Break();
#endif
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
                if(client == null)
                    Close();
                int bytesRead = client.EndReceive(ar);
                if(bytesRead > 0)
                {
                    Array.Resize(ref state.buffer, bytesRead);
                    // Add data to sb.
                    state.sb.Append(Convert.ToBase64String(state.buffer, 0, bytesRead));
                    if(state.sb.Length > 1)
                    {
#if DEBUG
                        //System.Console.WriteLine("doInput:" + state.sb.ToString());
#endif
                        onInput.Invoke(this, state);
                    }
                    //  Go get more data!
                    StartReceiving();
                }
                else
                {
                    // Got some data.
                    if(state.sb.Length > 1)
                    {
                        onInput.Invoke(this, state);
                    }
                    //Connection dropped, gtfo!
                    this.Close();
                }
            }
            catch(ObjectDisposedException oe)
            {
                this.Close();
            }
            catch(SocketException se)
            {
                this.Close();
            }
        }

        private void RegisterHandlers()
        {
            //HACK lol
#if DEBUG
            System.Console.WriteLine("Register Handlers");
#endif
            if(onError == null)
                onError += new dOnError(handleError);
            if(onInput == null)
                onInput += new dOnInput(handleInput);
            if(onConnectionEvent == null)
                onConnectionEvent += new dConnectionEvent(handleConnectionEvent);
        }

        private void UnregisterHandlers()
        {
            //HACK lol
#if DEBUG
            System.Console.WriteLine("Unregister handlers Handlers");
#endif
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
            SocketException se = e as SocketException;
            if(se != null)
            {
                if(se.ErrorCode == 10054 || se.SocketErrorCode == SocketError.ConnectionRefused || se.SocketErrorCode == SocketError.ConnectionAborted)
                {
                    //Means that the server disconnected, or we disconnected.
                    this.Close();
                }
                else
                {
#if DEBUG
                    System.Diagnostics.Debugger.Break();
#endif
                }
            }
        }

        /// <summary>
        /// Whenever data is received, you can always find it here!
        /// </summary>
        /// <param name="sm">Object that received the data.</param>
        /// <param name="data">byte array of data received.</param>
        /// <param name="size">Size of the byte array</param>
        protected virtual void handleInput(object Sender, ShitBag shit) { return; }

        /// <summary>
        /// Whenever this Connects or Disconnects, this is fired.
        /// </summary>
        /// <param name="Sender">Object that Connected or Disconnected.</param>
        /// <param name="e">Information about the Connection or Disconnection</param>
        /// <seealso cref="ConnectionEvent.cs"/>
        protected virtual void handleConnectionEvent(object Sender, ConnectionEvent e) { if(e.Event == ConnectionEvent.eConnectionEvent.eceDisconnect)UnregisterHandlers(); else bCalledClose = false; }
    }
}