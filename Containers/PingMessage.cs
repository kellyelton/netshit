using System;

namespace Skylabs.NetShit
{
    /// <summary>
    /// A version of SocketMessage that is used for pinging. It requires no header or arguments.
    /// </summary>
    [Serializable]
    public class PingMessage : SocketMessage
    {
        public String _Header = new String(new char[1] { (char)1 });
        private Boolean _forceFull = true;

        public override String getMessage()
        {
            String ret = "";
            ret += (char)1;
            return ret;
        }
    }
}