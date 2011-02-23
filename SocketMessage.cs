using System;
using System.Collections;
namespace Skylabs.NetShit
{
        /// <summary>
        /// A class used as the base of messages sent and recieved.
        /// It has variable, Empty, that if it is true, this class is treated as 
        /// being empty. If there is no Header information, the class is considered
        /// Empty, so make sure it has a header before you send anything. 
        /// </summary>
        public  class SocketMessage
        {
            public String Header
            {
                get
                {
                    if (_Header != null && !_Header.Trim().Equals(""))
                        return _Header;
                    else
                        return "";
                }
                set
                {
                    if (value != null && !value.Trim().Equals(""))
                    {
                        _Header = value;
                    }
                    else
                        _Header = "";
                }
            }
            public Boolean Empty
            {
                get
                {
                    if (_forceFull)
                        return false;
                    if (_Header != null && !_Header.Trim().Equals(""))
                        return false;
                    else
                        return true;
                }
            }
            public ArrayList Arguments;
            private String _Header;
            private Boolean _forceFull = false;
            public SocketMessage()
            {
                _Header = "";
                Arguments = new ArrayList(0);

            }
            public SocketMessage(String header)
            {
                _Header = "";
                Header = header;
                Arguments = new ArrayList(0);
            }
            /// <summary>
            /// Takes the message data and formats it into a string
            /// for transmitting.
            /// </summary>
            /// <returns>String for transmitting</returns>
            public virtual String getMessage()
            {
                String ret = "";
                if (Empty)
                    return ret;

                ret += (char)2;
                ret += _Header;
                if (Arguments.Count != 0)
                {
                    ret += (char)3;
                    for(int i=0;i<Arguments.Count;i++)
                    {
                        ret += (string)Arguments[i];
                        if (i != Arguments.Count - 1)
                            ret += (char)4;
                    }
                }
                ret += (char)5;
                return ret;

            }
        }
}

