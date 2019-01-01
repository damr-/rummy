using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace romme.Utility
{
    public static class Extensions
    {
        public static void Shuffle<T>(this IList<T> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = ThreadSafeRandom.ThisThreadsRandom.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }

        public static Stack<T> Shuffle<T>(this Stack<T> stack)
        {
            Random.InitState((int)(Time.time * 1000));
            return new Stack<T>(stack.OrderBy(x => Random.Range(0, int.MaxValue)));
        }
    }

}