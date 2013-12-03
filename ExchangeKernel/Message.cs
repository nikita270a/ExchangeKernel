using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExchangeKernel
{
    abstract class Message
    {
        const int register = 0;
        const int shutdown = 1;
        const int place    = 2;
        const int cancel   = 3;
        public static Message CreateMessage(byte[] msg)
        {
            int type = BitConverter.ToInt32(msg, 0);
            switch (type)
            {
                case register: return new RegisterMessage(msg);
                case shutdown: return new ShutDownMessage();
                case place:    return new PlaceMessage(msg);
                case cancel:   return new CancelMessage(msg);
                default: return null;
            }
        }
    }
    class PlaceMessage : Message
    {
        public string user_id;
        public MyTuple<long, long> price;
        public long quantity;
        public bool buy;
        public string asset1, asset2;
        public PlaceMessage(byte[] msg)
        {
            int len = BitConverter.ToInt32(msg, 4);
            user_id = System.Text.Encoding.ASCII.GetString(msg, 8, len);
            price = new MyTuple<long, long>(BitConverter.ToInt64(msg, len + 8), BitConverter.ToInt64(msg, len + 16));
            quantity = BitConverter.ToInt64(msg, len + 24);
            buy = BitConverter.ToInt32(msg, len + 32) > 0;
            int len2 = BitConverter.ToInt32(msg, len + 36);
            asset1 = System.Text.Encoding.ASCII.GetString(msg, len + 40, len2);
            int len3 = BitConverter.ToInt32(msg, len + len2 + 40);
            asset2 = System.Text.Encoding.ASCII.GetString(msg, len + len2 + 44, len3); 
        }
    }
    class CancelMessage : Message
    {
        public long id;
        public string user_id;
        public CancelMessage(byte[] msg)
        {
            id = BitConverter.ToInt64(msg, 4);
            int len = BitConverter.ToInt32(msg, 12);
            user_id = System.Text.Encoding.ASCII.GetString(msg, 16, len);
        }
    }
    class ShutDownMessage : Message
    {}
    class RegisterMessage : Message
    {
        public string login, password;
        public RegisterMessage(byte[] msg)
        {
            int len = BitConverter.ToInt32(msg, 4);
            login = System.Text.Encoding.ASCII.GetString(msg, 8, len);
            int len2 = BitConverter.ToInt32(msg, 8 + len);
            password = System.Text.Encoding.ASCII.GetString(msg, 12 + len, len2);
        }
    }
}
