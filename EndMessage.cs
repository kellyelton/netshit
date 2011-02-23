using System;
namespace Skylabs.NetShit
{
    
    public class EndMessage : SocketMessage
    {
        public String _Header = new String(new char[1] { (char)6 });
        private Boolean _forceFull = true;
        public override String getMessage()
        {
            String ret = "";
            ret += (char)6;
            return ret;
        }
    }
}

