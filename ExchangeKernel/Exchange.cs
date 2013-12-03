using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExchangeKernel
{
    class Exchange
    {
        Dictionary<string, User> users = new Dictionary<string, User>();
        Dictionary<string, Dictionary<string, SortedDictionary<MyTuple<long, long>, List<Order>>>> buy =
            new Dictionary<string, Dictionary<string, SortedDictionary<MyTuple<long, long>, List<Order>>>>();
        Dictionary<string, Dictionary<string, SortedDictionary<MyTuple<long, long>, List<Order>>>> sell =
            new Dictionary<string, Dictionary<string, SortedDictionary<MyTuple<long, long>, List<Order>>>>();
        Dictionary<long, Order> orders = new Dictionary<long, Order>();
        internal Exchange()
        {
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
        }
        internal bool RegisterUser(RegisterMessage msg)
        {
            if (!users.ContainsKey((msg as RegisterMessage).login))
            {
                users[(msg as RegisterMessage).login] = new User(msg as RegisterMessage);
                return true;
            }
            return false;
        }
        internal void ShutDown()
        {
            List<string> lines = new List<string>();
            foreach (User u in users.Values)
            {
                lines.Add(u.ToString());
            }
            System.IO.File.WriteAllLines("users.csv", lines);
        }
        internal int CancelOrder(CancelMessage cm)
        {
            bool ok = false;
            if (!users.ContainsKey(cm.user_id))
            {
                return -1;
            }
            if (orders[cm.id].buy)
            {
                for (int i = 0; i < buy[orders[cm.id].asset1][orders[cm.id].asset2][orders[cm.id].price].Count; ++i)
                {
                    if (buy[orders[cm.id].asset1][orders[cm.id].asset2][orders[cm.id].price][i].id == cm.id)
                    {
                        if (buy[orders[cm.id].asset1][orders[cm.id].asset2][orders[cm.id].price][i].user != cm.user_id)
                        {
                            return -1;
                        }
                        buy[orders[cm.id].asset1][orders[cm.id].asset2][orders[cm.id].price].RemoveAt(i);
                        ok = true;
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
                        if (sell[orders[cm.id].asset1][orders[cm.id].asset2][orders[cm.id].price][i].user != cm.user_id)
                        {
                            return -1;
                        }
                        sell[orders[cm.id].asset1][orders[cm.id].asset2][orders[cm.id].price].RemoveAt(i);
                        ok = true;
                        break;
                    }
                }
            }
            if (!ok)
            {
                return -2;
            }
            return 0;
        }

        internal Tuple<byte[], List<byte[]>> Place(PlaceMessage pm)
        {
            Order o = new Order(pm);
            Tuple<byte[], List<byte[]>> ans = new Tuple<byte[], List<byte[]>>(BitConverter.GetBytes(o.id), new List<byte[]>());
            ans.Item2.Add(o.AddedString());
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
                    ans.Item2.Add(tick.ToArray());
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
            return ans;
        }
    }
}
