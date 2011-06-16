using System;

//using System.Windows;
using System.Text;

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
            if (sendCloseMessage)
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
            byte[] mess = Encoding.ASCII.GetBytes(sm.getMessage());
            return WriteData(mess);
        }

        protected virtual void handleInput(object Sender, ShitBag shit)
        {
            SocketReadState sr = SocketReadState.WaitingForStart;
            String strBuff = "";
            foreach (char c in shit.buffer)
            {
                if (sr == SocketReadState.WaitingForStart || sr == SocketReadState.Reading)
                {
                    switch (sr)
                    {
                        case SocketReadState.WaitingForStart:
                            if (c == 2)//Started message
                            {
                                sr = SocketReadState.Reading;
                                continue;
                            }
                            else if (c == 1)//just a ping
                            {
                                doInput(new PingMessage());
                            }
                            else if (c == 6)//remote socket sent end message
                            {
                                Close(false);
                                //return new SocketMessage();
                            }
                            else if (c == 0)
                            {
                                //return new SocketMessage();
                            }
                            break;
                        case SocketReadState.Reading:
                            if (c != 5)//reading
                                strBuff += c;
                            else// c==5 so were done with the message.
                            {
                                sr = SocketReadState.Ended;
                            }
                            break;
                    }
                }
                if (sr == SocketReadState.Ended)
                {
                    SocketMessage sm = new SocketMessage();
                    String[] firstsplit = strBuff.Split(new char[1] { (char)3 });
                    sm.Header = firstsplit[0];
                    if (firstsplit.Length > 1)
                    {
                        String[] args = firstsplit[1].Split(new char[1] { (char)4 });
                        foreach (String a in args)
                        {
                            sm.Arguments.Add(a);
                        }
                    }
                    doInput(sm);
                    strBuff = "";
                    sm = new SocketMessage();
                    sr = SocketReadState.WaitingForStart;
                }
            }
        }

        protected virtual void handleInput(object Sender, SocketMessage mess) { }

        private void doInput(SocketMessage input)
        {
            if (!input.Empty)
            {
                handleInput(this, input);
                onSocketMessageInput.Invoke(this, input);
            }
            //TODO Put shit here.
        }
    }
}