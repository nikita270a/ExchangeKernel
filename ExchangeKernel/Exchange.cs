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
    }
}
