using System;

//using System.Windows;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Skylabs.NetShit
{
    public abstract class ShitSock
    {
        public TcpClient sock { get; set; }

        private IPEndPoint ipEnd;
        private Boolean boolEnd;
        private Boolean boolConnected;
        private String strDisconnectReason;
        private int intLastPing = 0;
        private DateTime LastPingSent;
        private DateTime LastPingRecieved;
        protected Thread oThread;
        protected String strHost;
        protected Int32 intPort;

        public Boolean Connected
        {
            get { return boolConnected; }
        }

        private enum SocketReadState
        {
            WaitingForStart, Reading, Ended, inHeader, inArgument
        }

        public void GetAcceptedSocket(TcpClient s)
        {
            try
            {
                strDisconnectReason = "";
                sock = s;
                boolConnected = false;
                boolEnd = false;
                IPEndPoint remoteIpEndPoint = s.Client.RemoteEndPoint as IPEndPoint;
                strHost = remoteIpEndPoint.Address.ToString();
                intPort = remoteIpEndPoint.Port;
                ipEnd = remoteIpEndPoint;
                sock.ReceiveTimeout = 10000;
                boolConnected = true;
                intLastPing = 0;
                LastPingSent = DateTime.Now;
                LastPingRecieved = DateTime.Now;
                sock.Client.Blocking = true;
                handleConnect(strHost, intPort);
                oThread = new Thread(new ThreadStart(this.run));
                oThread.Start();
            }
            catch (Exception e)
            {
                handleError(e, "Connect method: " + e.Message);
            }
        }

        /// <summary>
        /// Connect to a server.
        /// </summary>
        /// <param name="Host">Host name of the server</param>
        /// <param name="Port">Port of the server.</param>
        /// <returns>Boolean. True if connected, false if an error.</returns>
        public Boolean Connect(String Host, Int32 Port)
        {
            try
            {
                ipEnd = HostToEndpoint(Host, Port);
                if (ipEnd == null)
                {
                    return false;
                }
                strDisconnectReason = "";
                boolEnd = false;
                boolConnected = false;
                strHost = Host;
                intPort = Port;
                sock = new TcpClient();
                //sock = new Socket(ipEnd.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                sock.ReceiveTimeout = 10000;
                sock.Connect(ipEnd);
                sock.Client.Blocking = true;
                boolConnected = true;
                intLastPing = 0;
                handleConnect(Host, Port);
                oThread = new Thread(new ThreadStart(this.run));
                oThread.Start();
                LastPingSent = DateTime.Now;
                LastPingRecieved = DateTime.Now;
                return true;
            }
            catch (Exception e)
            {
                handleError(e, "Connect method: " + e.Message);
            }
            return false;
        }

        /// <summary>
        /// Close the connection to the server.
        /// </summary>
        /// <param name="reason">Reason for disconnecting. The reason is sent to the server.</param>
        /// <param name="sendCloseMessage">Should the socket attempt to notify the remote socket of the disconnect. Don't use if the connection has already been dropped.</param>
        public void Close(String reason, Boolean sendCloseMessage)
        {
            boolEnd = true;
            boolConnected = false;
            strDisconnectReason = reason;
            if (sendCloseMessage)
                writeMessage(new EndMessage());
            strDisconnectReason = reason;
        }

        /// <summary>
        ///    Client loop. Automates pinging to make sure the connection still exists.
        ///     This should be called as a thread because it's a loop.
        ///     THIS FUNCTION STARTS THE CLIENT AFTER CONNECTION, WITHOUT IT NOTHING HAPPENS.
        /// </summary>
        public void run()
        {
            try
            {
                while (!boolEnd)
                {
                    if (sock.Connected)
                    {
                        readSocket();
                    }
                    else
                        boolEnd = true;
                    if (boolEnd)
                        break;
                    try
                    {
                        TimeSpan ts = new TimeSpan(DateTime.Now.Ticks - LastPingRecieved.Ticks);
                        if (ts.TotalMinutes >= 1)
                        {
                            Close("Haven't recieved data in too long.", true);
                        }
                        ts = new TimeSpan(DateTime.Now.Ticks - LastPingSent.Ticks);
                        if (ts.TotalSeconds >= 20)
                        {
                            writeMessage(new PingMessage());
                        }
                        Thread.Sleep(100);
                    }
                    catch (Exception ie)
                    {
                        Close("Error: " + ie.Message, false);
                    }
                }
                try
                {
                    sock.Close();
                }
                catch (Exception ioe) { }
                try
                {
                    this.oThread.Join(1000);
                }
                catch (Exception e)
                { }
                handleDisconnect(strDisconnectReason, strHost, intPort);
                try
                {
                    this.oThread.Abort();
                }
                catch (Exception e)
                { }
            }
            catch (ThreadAbortException te)
            {
            }
            catch (Exception e)
            {
                handleError(e, "Unhandled exception");
            }
        }

        private void processMessage(SocketMessage sm)
        {
            if (!sm.Empty)
                handleInput(sm);
        }

        /// <summary>
        /// Reads data from socket if it's available, turns the data into a SocketMessage, and sends it to proccessMessage()
        /// </summary>
        private void readSocket()
        {
            StringBuilder sbuildmessage = new StringBuilder();
            NetworkStream stream = sock.GetStream();
            if (sock.Available > 0)
            {
                while (sock.Available > 0)
                {
                    int readAmount = sock.ReceiveBufferSize;
                    if (sock.ReceiveBufferSize > sock.Available)
                        readAmount = sock.Available;
                    byte[] bIn = new byte[readAmount];
                    stream.Read(bIn, 0, readAmount);
                    LastPingRecieved = DateTime.Now;
                    sbuildmessage.Append(Encoding.ASCII.GetString(bIn));
                }
                char[] letters = sbuildmessage.ToString().ToCharArray();
                SocketReadState sr = SocketReadState.WaitingForStart;
                String strBuff = "";
                foreach (char c in letters)
                {
                    if (sr == SocketReadState.WaitingForStart || sr == SocketReadState.Reading)
                    {
                        switch (sr)
                        {
                            case SocketReadState.WaitingForStart:
                                if (c == 2)//Started message
                                {
                                    sr = SocketReadState.Reading;
                                    continue;
                                }
                                else if (c == 1)//just a ping
                                {
                                    processMessage(new PingMessage());
                                }
                                else if (c == 6)//remote socket sent end message
                                {
                                    Close("Remote Host requested close.", false);
                                    //return new SocketMessage();
                                }
                                else if (c == 0)
                                {
                                    //return new SocketMessage();
                                }
                                break;
                            case SocketReadState.Reading:
                                if (c != 5)//reading
                                    strBuff += c;
                                else// c==5 so were done with the message.
                                {
                                    sr = SocketReadState.Ended;
                                }
                                break;
                        }
                    }
                    if (sr == SocketReadState.Ended)
                    {
                        SocketMessage sm = new SocketMessage();
                        String[] firstsplit = strBuff.Split(new char[1] { (char)3 });
                        sm.Header = firstsplit[0];
                        if (firstsplit.Length > 1)
                        {
                            String[] args = firstsplit[1].Split(new char[1] { (char)4 });
                            foreach (String a in args)
                            {
                                sm.Arguments.Add(a);
                            }
                        }
                        processMessage(sm);
                        strBuff = "";
                        sm = new SocketMessage();
                        sr = SocketReadState.WaitingForStart;
                    }
                }
            }
            else
            {
                Thread.Sleep(100);
            }
        }

        /// <summary>
        /// Sends a message to the remote socket.
        /// </summary>
        /// <param name="sm">SocketMessage to be sent.</param>
        /// <returns>true on success, false on error. Note: Success just means that the message has been sent, it doesn't verifiy it was recieved.</returns>
        public Boolean writeMessage(SocketMessage sm)
        {
            try
            {
                byte[] mess = Encoding.ASCII.GetBytes(sm.getMessage());
                sock.GetStream().Write(mess, 0, mess.Length);
                LastPingSent = DateTime.Now;
                return true;
            }
            catch (SocketException se)
            {
                //handleError(se, se.SocketErrorCode + " : " + se.Message);
            }
            catch (IOException ioe)
            {
                Close("IOException, connection closed.", false);
            }
            catch (NullReferenceException nre)
            {
            }
            catch (ObjectDisposedException oe)
            {
                Close("Connection closed, could not GetStream()", false);
            }
            catch (Exception ioe)
            {
                handleError(ioe, ioe.Message);
            }
            return false;
        }

        /// <summary>
        /// Converts a string host, such as "www.google.com" or "localhost" to an IPEndPoint.
        /// </summary>
        /// <param name="hostName">Host name, such as "www.google.com" or "localhost"</param>
        /// <param name="port">Port of the server.</param>
        /// <returns>System.Net.IPEndPoint, or NULL on error.</returns>
        public IPEndPoint HostToEndpoint(String hostName, Int32 port)
        {
            try
            {
                IPHostEntry host = Dns.GetHostEntry(hostName);

                // Addres of the host.
                IPAddress[] addressList = host.AddressList;

                // Instantiates the endpoint and socket.
                return new IPEndPoint(addressList[addressList.Length - 1], port);
            }
            catch { }
            return null;
        }

        /// <summary>
        /// Called when there is an error in the SocketClient class.
        /// </summary>
        /// <param name="error">String representation of the error.</param>
        public abstract void handleError(Exception e, String error);

        /// <summary>
        /// Called when the server sends data that isn't intercepted by the Socket Client class.
        /// </summary>
        /// <param name="input">Data sent from the server as a String</param>
        public abstract void handleInput(SocketMessage input);

        /// <summary>
        /// Called when the client connects to the server
        /// </summary>
        /// <param name="host">Host name of the server</param>
        /// <param name="port">Port of the server.</param>
        public abstract void handleConnect(String host, int port);

        /// <summary>
        /// Called when the connection to the server is closed for any reason.
        /// </summary>
        /// <param name="reason">String from eather the Close() method or from the server explaining why the connection was dropped.</param>
        /// <param name="host">Host name of the server</param>
        /// <param name="port">Port of the server.</param>
        public abstract void handleDisconnect(String reason, String host, int port);
    }
}