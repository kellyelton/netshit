﻿using System.Net.Sockets;
using System.Text;

namespace Skylabs.NetShit
{
    public class ShitBag
    {
        // Client socket.
        public Socket workSocket = null;
        // Size of receive buffer.
        public const int BufferSize =1048576;
        // Receive buffer.
        public byte[] buffer = new byte[BufferSize];
        // Received data string.
        public StringBuilder sb = new StringBuilder();
    }
}