using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace ExchangeKernel
{
    internal class DataPlaceMessage
    {
        public string user_id;
        public MyTuple<long, long> price;
        public long quantity;
        public bool buy;
        public string asset1;
        public string asset2;


        public List<byte> msg = new List<byte>();

        void ComposePlaceMessage(string user_id, MyTuple<long, long> price, long quantity, bool buy, string asset1, string
            asset2)
        {
            msg.AddRange(BitConverter.GetBytes(2));//Place 
            msg.AddRange(BitConverter.GetBytes(strlen(user_id)));
            msg.AddRange(System.Text.Encoding.ASCII.GetBytes(user_id));
            msg.AddRange(BitConverter.GetBytes(price.Item1));
            msg.AddRange(BitConverter.GetBytes(price.Item2));
            msg.AddRange(BitConverter.GetBytes(quantity));
            msg.AddRange(BitConverter.GetBytes(buy));
            msg.AddRange(BitConverter.GetBytes(strlen(asset1)));
            msg.AddRange(System.Text.Encoding.ASCII.GetBytes(asset1));
            msg.AddRange(BitConverter.GetBytes(strlen(asset2)));
            msg.AddRange(System.Text.Encoding.ASCII.GetBytes(asset2));
        }
    }


}






