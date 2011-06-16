using System;
using System.Net;
using System.Net.Sockets;
using Skylabs.NetShit;

namespace NetShit
{
    public class Forwarder
    {
        private int _listenPort;
        private string _forwardToHost;
        private int _forwardToPort;
        private TcpListener _listener;
        private RawShitSock _rSock;
        private RawShitSock _lSock;

        public bool Connected
        {
            get
            {
                //Making if statements confusing is fun!
                if(_rSock != null) if(_rSock.Connected) if(_lSock != null) if(_lSock.Connected)
                                return true;
                return false;
            }
        }

        public int ListenPort
        {
            get { return _listenPort; }
            private set { _listenPort = value; }
        }

        public string ForwardToHost
        {
            get { return _forwardToHost; }
            private set { _forwardToHost = value; }
        }

        public int ForwardToPort
        {
            get { return _forwardToPort; }
            private set { _forwardToPort = value; }
        }

        public Forwarder(int listenPort, string forwardToHost, int forwardToPort)
        {
            _listenPort = listenPort;
            _forwardToHost = forwardToHost;
            _forwardToPort = forwardToPort;
            _listener = null;
            _rSock = null;
            _lSock = null;
        }

        private TcpListener Listener
        {
            get { return _listener; }
            set { _listener = value; }
        }

        public void Start()
        {
            if(_listenPort == -1 || String.IsNullOrEmpty(_forwardToHost) || _forwardToPort == -1)
                throw (new HeyDummyInitializeYouVariablesBeforeYouTryAndUseThemDUH("You didn't initialize Forwarder..."));
            _listener = new TcpListener(IPAddress.Loopback, _listenPort);
            _listener.Start(1);
            _listener.BeginAcceptTcpClient(new AsyncCallback(ReceiveCallback), _listener);
        }

        /// <summary>
        /// Wreck everything(figuratively, it actually just resets the Forwarder)
        /// </summary>
        public void Disimboul()
        {
            _listener.Stop();
            _rSock.Close();
            _lSock.Close();
            _listenPort = -1;
            _forwardToHost = "";
            _forwardToPort = -1;
            _listener = null;
            _rSock = null;
            _lSock = null;
        }

        private void ReceiveCallback(IAsyncResult AsyncCall)
        {
            //Accept the connection.
            TcpListener listener = (TcpListener)AsyncCall.AsyncState;
            SetupSocks();
            _rSock.GetAcceptedSocket(listener.EndAcceptTcpClient(AsyncCall));
            //Connect to the Destination
            _lSock.Connect(_forwardToHost, _forwardToPort);
        }

        private void SetupSocks()
        {
            _rSock = new ShitSock();
            _lSock = new ShitSock();
            _rSock.onInput += new RawShitSock.dOnInput(_rSock_onInput);
            _lSock.onInput += new RawShitSock.dOnInput(_lSock_onInput);
            _rSock.onConnectionEvent += new RawShitSock.dConnectionEvent(_rSock_onConnectionEvent);
            _lSock.onConnectionEvent += new RawShitSock.dConnectionEvent(_lSock_onConnectionEvent);
        }

        private void _lSock_onConnectionEvent(object Sender, ConnectionEvent e)
        {
            if(e.Event == ConnectionEvent.eConnectionEvent.eceDisconnect)
            {
                _rSock.Close();
            }
        }

        private void _rSock_onConnectionEvent(object Sender, ConnectionEvent e)
        {
            if(e.Event == ConnectionEvent.eConnectionEvent.eceDisconnect)
            {
                _lSock.Close();
            }
        }

        private void _lSock_onInput(object sm, ShitBag bag)
        {
            _rSock.WriteData(bag.buffer);
        }

        private void _rSock_onInput(object sm, ShitBag bag)
        {
            _lSock.WriteData(bag.buffer);
        }
    }
}