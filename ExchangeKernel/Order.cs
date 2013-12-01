using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ExchangeKernel
{
    class Order
    {
        public string asset1, asset2;
        public string user;
        public MyTuple<long, long> price;
        public long quantity;
        public long id;
        public bool buy;
        private static long last_id = 0;
        public Order(PlaceMessage msg)
        {
            asset1 = msg.asset1;
            asset2 = msg.asset2;
            user = msg.user_id;
            price = msg.price;
            quantity = msg.quantity;
            buy = msg.buy;
            id = last_id++;
        }
        public byte[] AddedString()
        {
            List<byte> ans = new List<byte>();
            ans.AddRange(System.Text.Encoding.ASCII.GetBytes(asset1 + "/" + asset2));
            ans.Add(1);
            ans.AddRange(BitConverter.GetBytes(id));
            ans.Add((byte)(buy ? 1 : 0));
            ans.AddRange(BitConverter.GetBytes(price.Item1));
            ans.AddRange(BitConverter.GetBytes(price.Item2));
            ans.AddRange(BitConverter.GetBytes(quantity));
            return ans.ToArray();
        }
        public byte[] RemovedString()
        {
            List<byte> ans = new List<byte>();
            ans.AddRange(System.Text.Encoding.ASCII.GetBytes(asset1 + "/" + asset2));
            ans.Add(0);
            ans.AddRange(BitConverter.GetBytes(id));
            ans.Add((byte)(buy ? 1 : 0));
            ans.AddRange(BitConverter.GetBytes(price.Item1));
            ans.AddRange(BitConverter.GetBytes(price.Item2));
            ans.AddRange(BitConverter.GetBytes(quantity));
            return ans.ToArray();
        }
    }
}
