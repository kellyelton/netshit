using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace Skylabs.NetShit
{
    /// <summary>
    /// A class used as the base of messages sent and recieved.
    /// It has variable, Empty, that if it is true, this class is treated as
    /// being empty. If there is no Header information, the class is considered
    /// Empty, so make sure it has a header before you send anything.
    /// </summary>
    [Serializable()]
    public partial class SocketMessage
    {
        public String Header
        {
            get
            {
                if(_Header != null && !_Header.Trim().Equals(""))
                    return _Header;
                else
                    return "";
            }
            set
            {
                if(value != null && !value.Trim().Equals(""))
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
                if(_forceFull)
                    return false;
                if(_Header != null && !_Header.Trim().Equals(""))
                    return false;
                else
                    return true;
            }
        }

        public List<String> Arguments;
        private String _Header;
        private Boolean _forceFull = false;
        protected IFormatter iformatter;

        public SocketMessage()
        {
            _Header = "";
            Arguments = new List<String>(0);
        }

        public SocketMessage(String header)
        {
            _Header = "";
            Header = header;
            Arguments = new List<String>(0);
        }

        /// <summary>
        /// Takes the message data and formats it into a string
        /// for transmitting.
        /// </summary>
        /// <returns>String for transmitting</returns>
        public virtual String getMessage()
        {
            //return Convert.ToBase64String(SocketMessage.Serialize(this));
            String ret = "";
            if(Empty)
                return ret;

            ret += (char)2;
            ret += _Header;
            if(Arguments.Count != 0)
            {
                ret += (char)3;
                for(int i=0; i < Arguments.Count; i++)
                {
                    ret += (string)Arguments[i];
                    if(i != Arguments.Count - 1)
                        ret += (char)4;
                }
            }
            ret += (char)5;
            return ret;
        }

        public static SocketMessage DeSerialize(byte[] data)
        {
            if(data.Length > 0)
            {
                MemoryStream ms = new MemoryStream(data);
                BinaryFormatter bff = new BinaryFormatter();
                return (SocketMessage)bff.Deserialize(ms);
            }
            return new SocketMessage();
        }

        public static byte[] Serialize(SocketMessage obj)
        {
            BinaryFormatter bff = new BinaryFormatter();
            MemoryStream ms = new MemoryStream();
            bff.Serialize(ms, obj);
            return ms.ToArray();
        }
    }
}