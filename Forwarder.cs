using System;
using Skylabs.NetShit;

namespace NetShit
{
    public class Forwarder : ShitSock
    {
        private int _listenPort;
        private string _forwardToHost;
        private int _forwardToPort;

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
        }

        public void Start()
        {
        }

        protected override void handleError(ShitSock sm, Exception e, string error)
        {
            throw new NotImplementedException();
        }

        protected override void handleInput(ShitSock sm, SocketMessage input)
        {
            throw new NotImplementedException();
        }

        protected override void handleConnect(ShitSock sm, string host, int port)
        {
            throw new NotImplementedException();
        }

        protected override void handleDisconnect(ShitSock sm, string reason, string host, int port)
        {
            throw new NotImplementedException();
        }
    }
}