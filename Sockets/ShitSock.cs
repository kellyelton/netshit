using System;
using System.Collections.Generic;

//using System.Windows;

namespace Skylabs.NetShit
{
    public class ShitSock : RawShitSock
    {
        public delegate void dOnSockMessageInput(object Sender, SocketMessage sm);
        public event dOnSockMessageInput onSocketMessageInput;
        private List<byte> buffer = new List<byte>();

        private enum SocketReadState
        {
            WaitingForStart, Reading, Ended, inHeader, inArgument
        }

        /// <summary>
        /// Close the connection to the server.
        /// </summary>
        /// <param name="sendCloseMessage">Should the socket attempt to notify the remote socket of the disconnect. Don't use if the connection has already been dropped.</param>
        public void Close(Boolean sendCloseMessage)
        {
            if(sendCloseMessage)
                writeMessage(new EndMessage());
            base.Close();
        }

        /// <summary>
        /// Sends a message to the remote socket.
        /// </summary>
        /// <param name="sm">SocketMessage to be sent.</param>
        /// <returns>true on success, false on error. Note: Success just means that the message has been sent, it doesn't verifiy it was recieved.</returns>
        public Boolean writeMessage(SocketMessage sm)
        {
            List<byte> temp = new List<byte>(SocketMessage.Serialize(sm));
            byte hbyte =System.Text.ASCIIEncoding.ASCII.GetBytes("#")[0];
            for(int i=0; i < 5; i++)
                temp.Add(hbyte);
            return WriteData(temp.ToArray());
        }

        override protected void handleInput(object Sender, ShitBag shit)
        {
            buffer.AddRange(shit.buffer);
            process_buffer();
        }

        private void process_buffer()
        {
            List<byte> temp = new List<byte>();
            int hashcount = 0;
            byte hbyte =System.Text.ASCIIEncoding.ASCII.GetBytes("#")[0];
            for(int i=0; i < buffer.Count; i++)
            {
                if(hashcount == 5)
                {
                    buffer.RemoveRange(0, temp.Count);
                    temp.RemoveRange(temp.Count - 5, 5);
                    doInput(SocketMessage.DeSerialize(temp.ToArray()));
                    if(buffer.Count > 0)
                        process_buffer();
                    return;
                }
                temp.Add(buffer[i]);
                if(temp[i] == hbyte)
                {
                    hashcount++;
                }
                else
                    hashcount = 0;
            }
            if(hashcount == 5)
            {
                buffer.RemoveRange(0, temp.Count);
                temp.RemoveRange(temp.Count - 5, 5);
                doInput(SocketMessage.DeSerialize(temp.ToArray()));
                if(buffer.Count > 0)
                    process_buffer();
            }
        }

        protected virtual void handleInput(object Sender, SocketMessage mess) { }

        private void doInput(SocketMessage input)
        {
            if(!input.Empty)
            {
                handleInput(this, input);
                if(onSocketMessageInput != null)
                    onSocketMessageInput.Invoke(this, input);
            }
        }
    }
}