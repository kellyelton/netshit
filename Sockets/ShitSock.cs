using System;

//using System.Windows;

namespace Skylabs.NetShit
{
    public class ShitSock : RawShitSock
    {
        public delegate void dOnSockMessageInput(object Sender, SocketMessage sm);
        public event dOnSockMessageInput onSocketMessageInput;

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
            return WriteData(SocketMessage.Serialize(sm));
        }

        override protected void handleInput(object Sender, ShitBag shit)
        {
            SocketMessage sm = SocketMessage.DeSerialize(shit.buffer);
            if(sm == null)
            {
                doInput(new PingMessage());
            }
            else
            {
                doInput(sm);
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