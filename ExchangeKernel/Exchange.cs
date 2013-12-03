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
    }
}
