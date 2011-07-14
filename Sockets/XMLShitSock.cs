using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Xml;

namespace Skylabs.NetShit.Sockets
{
    public class XMLShitSock
    {
        private bool _connected;
        private IPEndPoint _ipEndPoint;
        private string _hostname;
        private int _port;
        private bool bCalledClose = false;
        public delegate void dOnError(object Sender, Exception e, String error);
        public delegate void dOnInput(object Sender, XmlDocument data);
        public delegate void dConnectionEvent(object Sender, ConnectionEvent e);
        public event dOnError onError;
        public event dOnInput onInput;
        public event dConnectionEvent onConnectionEvent;

        private Thread thread;

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
            return false;
        }

        public bool GetAcceptedSocket(TcpClient s)
        {
            RegisterHandlers();
            ShittySocket = s;
            IPendpoint = ShittySocket.Client.RemoteEndPoint as IPEndPoint;
            bCalledClose = false;
            _hostname = IPendpoint.Address.ToString();
            _port = IPendpoint.Port;
            _connected = true;
            onConnectionEvent.Invoke(this, new ConnectionEvent(_hostname, _port, ConnectionEvent.eConnectionEvent.eceConnect));
            StartReceiving();
            return true;
        }

        public void Close()
        {
            if(!bCalledClose)
            {
                bCalledClose = true;
                if(ShittySocket.Client != null)
                {
                    if(ShittySocket.Connected)
                    {
                        try
                        {
                            ShittySocket.Close();
                        }
                        catch(SocketException se) { }
                    }
                }
                _connected = false;
                onConnectionEvent(this, new ConnectionEvent(_hostname, _port, ConnectionEvent.eConnectionEvent.eceDisconnect));
                UnregisterHandlers();
            }
        }

        public bool WriteXMLMessage(XmlDocument doc)
        {
            try
            {
                ShittySocket.Client.Send(Encoding.ASCII.GetBytes(GetXmlString(doc)));
                return true;
            }
            catch(SocketException se)
            {
                this.Close();
                return false;
            }
        }

        public static string GetXmlString(XmlDocument doc)
        {
            StringWriter sw = new StringWriter();
            XmlTextWriter xw = new XmlTextWriter(sw);
            doc.WriteTo(xw);
            return sw.ToString();
        }

        protected virtual void StartReceiving()
        {
            thread = new Thread(new ThreadStart(doInput));
            thread.Start();
        }

        private void doInput()
        {
            using(NetworkStream ns = new NetworkStream(ShittySocket.Client, false))
            {
                using(StreamReader sr = new StreamReader(ns))
                {
                    while(_connected)
                    {
                        if(ns.DataAvailable)
                        {
                            XmlDocument xdoc = new XmlDocument();
                            while(xdoc != null)
                            {
                                xdoc = GrabXmlDocFromStream(sr);
                                if(xdoc != null)
                                    onInput(this, xdoc);
                            }
                        }
                        Thread.Sleep(50);
                    }
                }
            }
        }

        public static XmlDocument GrabXmlDocFromStream(StreamReader reader)
        {
            using(MemoryStream ms = new MemoryStream())
            {
                XmlReaderSettings xrs = new XmlReaderSettings();
                xrs.ConformanceLevel = ConformanceLevel.Fragment;

                int i = 1;
                byte b;
                try
                {
                    while(!reader.EndOfStream)
                    {
                        i = reader.Read();
                        if(i > 0)
                            b = (byte)i;
                        else
                            break;
                        if(ms.Length > 0)
                            ms.Position = ms.Length;
                        ms.WriteByte(b);
                        try
                        {
                            if(Encoding.ASCII.GetChars(new byte[1] { b })[0] == '>')
                            {
                                string s = Encoding.ASCII.GetString(ms.ToArray());
                                ms.Position = 0;
                                using(XmlReader xmlreader = XmlTextReader.Create(ms, xrs))
                                {
                                    if(xmlreader.Read())
                                    {
                                        XmlDocument objXmlDocument = new XmlDocument();
                                        objXmlDocument.Load(xmlreader);
                                        return objXmlDocument;
                                    }
                                }
                            }
                        }
                        catch(XmlException xe)
                        {
#if(DEBUG)
                            //System.Diagnostics.Debugger.Break();
#endif
                        }
                    }
                }
                catch(IOException e)
                {
                }
            }
            return null;
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
        protected virtual void handleInput(object Sender, XmlDocument shit) { return; }

        /// <summary>
        /// Whenever this Connects or Disconnects, this is fired.
        /// </summary>
        /// <param name="Sender">Object that Connected or Disconnected.</param>
        /// <param name="e">Information about the Connection or Disconnection</param>
        /// <seealso cref="ConnectionEvent.cs"/>
        protected virtual void handleConnectionEvent(object Sender, ConnectionEvent e) { }
    }
}