using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExchangeKernel
{
    class User
    {
        public string login;
        public string password;
        public List<Tuple<long, long>> eq;

        static readonly string[] currencies;
        static readonly Dictionary<string, int> num;

        const int MAGIC = 100 * 1000 * 1000;
        static User()
        {
            currencies = "RUR\tUSD\tEUR\tBTC\tLTC".Split('\t');
            num = new Dictionary<string, int>();
            for (int i = 0; i < currencies.Length; ++i)
            {
                num[currencies[i]] = i;
            }
        }
        public User(string s)
        {
            string[] parts = s.Split(';');
            login = parts[0];
            password = parts[1];
            for (int i = 2; i < parts.Length; ++i)
            {
                string[] parts2 = parts[i].Split('.');
                eq.Add(new Tuple<long, long>(Convert.ToInt64(parts2[0]), Convert.ToInt64(parts2[1])));
            }
            while (eq.Count < currencies.Length)
            {
                eq.Add(new Tuple<long, long>(0, 0));
            }
        }
        public User(RegisterMessage msg)
        {
            login = msg.login;
            password = msg.password;
            eq = new List<Tuple<long, long>>();
            while (eq.Count < currencies.Length)
            {
                eq.Add(new Tuple<long, long>(0, 0));
            }
        }
        public void AddAsset(string asset, long quantity)
        {
            long i = eq[num[asset]].Item1;
            long r = eq[num[asset]].Item2;
            i += quantity / MAGIC;
            r += quantity % MAGIC;
            if (r >= MAGIC)
            {
                r -= MAGIC;
                ++i;
            }
            if (r < 0)
            {
                --i;
                r += MAGIC;
            }
            eq[num[asset]] = new Tuple<long, long>(i, r);
        }
        public void AddCurrency(string asset, long quantity, MyTuple<long, long> price)
        {
            long q1, q2;
            MUL(price, quantity, out q1, out q2);
            long i = eq[num[asset]].Item1;
            long r = eq[num[asset]].Item2;
            i += q1;
            r += q2;
            if (r >= MAGIC)
            {
                r -= MAGIC;
                ++i;
            }
            if (r < 0)
            {
                --i;
                r += MAGIC;
            }
            eq[num[asset]] = new Tuple<long, long>(i, r);
        }
        private void MUL(MyTuple<long, long> price, long quantity, out long q1, out long q2)
        {
            q1 = price.Item1 * quantity + price.Item2 * (quantity / MAGIC) + price.Item2 * (quantity % MAGIC) / MAGIC;
            q2 = price.Item2 * (quantity % MAGIC) % MAGIC;
        }
        public override string ToString()
        {
            string ans = login + ';' + password;
            foreach (Tuple<long, long> t in eq)
            {
                ans += ';' + t.Item1 + '.' + t.Item2;
            }
            return ans;
        }
    }
}
