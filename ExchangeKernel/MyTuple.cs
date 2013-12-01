using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ExchangeKernel
{
    class MyTuple<T1, T2> : IComparable where T1 : IComparable where T2 : IComparable
    {
        public T1 Item1;
        public T2 Item2;
        public int CompareTo(object obj)
        {
            MyTuple<T1, T2> o2 = obj as MyTuple<T1, T2>;
            int d = Item1.CompareTo(o2.Item1);
            if (d != 0)
            {
                return d;
            }
            return Item2.CompareTo(o2.Item2);
        }
        public MyTuple(T1 lhs, T2 rhs)
        {
            Item1 = lhs;
            Item2 = rhs;
        }
    }
}
