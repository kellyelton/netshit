using System;

namespace Skylabs.NetShit
{
    public class ConnectionEvent
    {
        public eConnectionEvent Event { get; private set; }

        public String Host { get; private set; }

        public int Port { get; private set; }

        public String XData { get; private set; }

        public enum eConnectionEvent { eceConnect, eceDisconnect };

        public ConnectionEvent(String host, int port, eConnectionEvent e)
        {
            Host = host;
            Port = port;
            Event = e;
        }

        public ConnectionEvent(String host, int port, String xdata, eConnectionEvent e)
        {
            Host = host;
            Port = port;
            Event = e;
            XData = xdata;
        }

        public static bool operator ==(ConnectionEvent e1, ConnectionEvent e2)
        {
            if(e1.Event == e2.Event)
                return true;
            return false;
        }

        public static bool operator !=(ConnectionEvent e1, ConnectionEvent e2)
        {
            return ((e1 == e2) == false);
        }
    }
}