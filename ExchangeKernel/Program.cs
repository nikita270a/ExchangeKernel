using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExchangeKernel
{
    class Program
    {
        static ZMQ.Context cntx = new ZMQ.Context();
        static ZMQ.Socket rep, pub;
        static Exchange ex = new Exchange();

        #region error codes
        static byte[] OK = new byte[1];
        static byte[] ALREADY_HERE = new byte[1];
        static byte[] USER_NOT_FOUND = new byte[1];
        static byte[] ORDER_NOT_FOUND = new byte[1];
        #endregion
        static private void InitErrMessages()
        {
            ALREADY_HERE[0] = 1;
            USER_NOT_FOUND[0] = 2;
            ORDER_NOT_FOUND[0] = 2;
        }
        static void Main(string[] args)
        {
            InitErrMessages();
            rep = cntx.Socket(ZMQ.SocketType.REP);
            rep.Bind("tcp://*:1000");
            pub = cntx.Socket(ZMQ.SocketType.PUB);
            pub.Bind("tcp://*:1001");
            while (true)
            {
                byte[] buf = rep.Recv(0);
                if (buf != null)
                {
                    Process(buf);
                }
            }
        }
        private static void Process(byte[] buf)
        {
            Message msg = Message.CreateMessage(buf);
            if (msg == null)
            {
                rep.Send("Unrecognized command", System.Text.Encoding.ASCII);
                return;
            }
            if (msg is RegisterMessage)
            {
                if (ex.RegisterUser(msg as RegisterMessage))
                {
                    rep.Send(OK);
                }
                else
                {
                    rep.Send(ALREADY_HERE);
                }
            }
            if (msg is ShutDownMessage)
            {
                ex.ShutDown();
                rep.Send(OK);
            }
            if (msg is PlaceMessage)
            {
                Tuple<byte[], List<byte[]>> send = ex.Place(msg as PlaceMessage);
                rep.Send(send.Item1);
                foreach (byte[] b in send.Item2)
                {
                    pub.Send(b);
                }
            }
            if (msg is CancelMessage)
            {
                int r = ex.CancelOrder(msg as CancelMessage);
                switch (r)
                {
                    case -1: rep.Send(USER_NOT_FOUND);
                        break;
                    case -2: rep.Send(ORDER_NOT_FOUND);
                        break;
                    default: rep.Send(OK);
                        break;
                }
            }
        }
    }
}
