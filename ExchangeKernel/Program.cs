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
        static Dictionary<string, User> users = new Dictionary<string, User>();
        static Dictionary<string, Dictionary<string, SortedDictionary<MyTuple<long, long>, List<Order>>>> buy =
            new Dictionary<string, Dictionary<string, SortedDictionary<MyTuple<long, long>, List<Order>>>>();
        static Dictionary<string, Dictionary<string, SortedDictionary<MyTuple<long, long>, List<Order>>>> sell =
            new Dictionary<string, Dictionary<string, SortedDictionary<MyTuple<long, long>, List<Order>>>>();
        static Dictionary<long, Order> orders = new Dictionary<long, Order>();

        #region error codes
        static byte[] OK = new byte[1];
        static byte[] ALREADY_HERE = new byte[1];
        #endregion
        static private void InitErrMessages()
        {
            ALREADY_HERE[0] = 1;
        }
        static void Main(string[] args)
        {
            InitErrMessages();
            try
            {
                string[] lines = System.IO.File.ReadAllLines("users.csv");
                foreach (string line in lines)
                {
                    users[line.Split(';')[0]] = new User(line);
                }
            }
            catch (Exception)
            { }
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
                if (!users.ContainsKey((msg as RegisterMessage).login))
                {
                    users[(msg as RegisterMessage).login] = new User(msg as RegisterMessage);
                    rep.Send(OK);
                }
                else
                {
                    rep.Send(ALREADY_HERE);
                }

            }
            if (msg is ShutDownMessage)
            {
                List<string> lines = new List<string>();
                foreach (User u in users.Values)
                {
                    lines.Add(u.ToString());
                }
                System.IO.File.WriteAllLines("users.csv", lines);
                rep.Send(OK);
            }
            if (msg is PlaceMessage)
            {
                PlaceMessage pm = msg as PlaceMessage;
                Order o = new Order(pm);
                rep.Send(BitConverter.GetBytes(o.id));
                pub.Send(o.AddedString());
                orders[o.id] = o;
                if (pm.buy)
                {
                    if (!buy.ContainsKey(pm.asset1))
                    {
                        buy[pm.asset1] = new Dictionary<string, SortedDictionary<MyTuple<long, long>, List<Order>>>();
                    }
                    if (!buy[pm.asset1].ContainsKey(pm.asset2))
                    {
                        buy[pm.asset1][pm.asset2] = new SortedDictionary<MyTuple<long, long>, List<Order>>();
                    }
                    if (!buy[pm.asset1][pm.asset2].ContainsKey(pm.price))
                    {
                        buy[pm.asset1][pm.asset2][pm.price] = new List<Order>();
                    }
                    buy[pm.asset1][pm.asset2][pm.price].Add(o);
                }
                else
                {
                    if (!buy.ContainsKey(pm.asset1))
                    {
                        buy[pm.asset1] = new Dictionary<string, SortedDictionary<MyTuple<long, long>, List<Order>>>();
                    }
                    if (!buy[pm.asset1].ContainsKey(pm.asset2))
                    {
                        buy[pm.asset1][pm.asset2] = new SortedDictionary<MyTuple<long, long>, List<Order>>();
                    }
                    if (!buy[pm.asset1][pm.asset2].ContainsKey(pm.price))
                    {
                        buy[pm.asset1][pm.asset2][pm.price] = new List<Order>();
                    }
                    buy[pm.asset1][pm.asset2][pm.price].Add(o);
                }
                while (sell[pm.asset1][pm.asset2].First().Key.CompareTo(buy[pm.asset1][pm.asset2].Last().Key) <= 0)
                {
                    MyTuple<long, long> price = pm.buy ? sell[pm.asset1][pm.asset2].First().Key : buy[pm.asset1][pm.asset2].First().Key;
                    List<Order> l1 = sell[pm.asset1][pm.asset2].First().Value;
                    List<Order> l2 = buy[pm.asset1][pm.asset2].Last().Value;
                    while (l1.Count > 0 && l2.Count > 0)
                    {
                        long q = Math.Min(l1[0].quantity, l2[0].quantity);
                        users[l1[0].user].AddAsset(pm.asset1, -q);
                        users[l2[0].user].AddAsset(pm.asset1, q);
                        users[l1[0].user].AddCurrency(pm.asset2, q, price);
                        users[l2[0].user].AddCurrency(pm.asset2, -q, price);
                        l1[0].quantity -= q;
                        l2[0].quantity -= q;
                        List<byte> tick = new List<byte>();
                        tick.AddRange(System.Text.Encoding.ASCII.GetBytes(pm.asset1 + "/" + pm.asset2));
                        tick.Add(2);
                        tick.AddRange(BitConverter.GetBytes(l1[0].id));
                        tick.AddRange(BitConverter.GetBytes(l2[0].id));
                        tick.AddRange(BitConverter.GetBytes(price.Item1));
                        tick.AddRange(BitConverter.GetBytes(price.Item2));
                        tick.AddRange(BitConverter.GetBytes(q));
                        pub.Send(tick.ToArray());
                        if (l1[0].quantity == 0)
                        {
                            l1.RemoveAt(0);
                        }
                        if (l2[0].quantity == 0)
                        {
                            l2.RemoveAt(0);
                        }
                    }
                    if (l1.Count == 0)
                    {
                        sell[pm.asset1][pm.asset2].Remove(sell[pm.asset1][pm.asset2].First().Key);
                    }
                    if (l2.Count == 0)
                    {
                        buy[pm.asset1][pm.asset2].Remove(buy[pm.asset1][pm.asset2].Last().Key);
                    }
                }
            }
            if (msg is CancelMessage)
            {
                CancelMessage cm = msg as CancelMessage;
                if (orders[cm.id].buy)
                {
                    for (int i = 0; i < buy[orders[cm.id].asset1][orders[cm.id].asset2][orders[cm.id].price].Count; ++i)
                    {
                        if (buy[orders[cm.id].asset1][orders[cm.id].asset2][orders[cm.id].price][i].id == cm.id)
                        {
                            buy[orders[cm.id].asset1][orders[cm.id].asset2][orders[cm.id].price].RemoveAt(i);
                            break;
                        }
                    }
                }
                else
                {
                    for (int i = 0; i < sell[orders[cm.id].asset1][orders[cm.id].asset2][orders[cm.id].price].Count; ++i)
                    {
                        if (sell[orders[cm.id].asset1][orders[cm.id].asset2][orders[cm.id].price][i].id == cm.id)
                        {
                            sell[orders[cm.id].asset1][orders[cm.id].asset2][orders[cm.id].price].RemoveAt(i);
                            break;
                        }
                    }
                }
                rep.Send(OK);
            }
        }
    }
}
