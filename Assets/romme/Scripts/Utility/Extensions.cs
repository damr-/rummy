using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace romme.Utility
{
    public static class Extensions
    {
        public static int Seed;

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
            Random.InitState(Seed);
            return new Stack<T>(stack.OrderBy(x => Random.Range(0, int.MaxValue)));
        }

        public static IDictionary<Card.CardNumber, List<Card>> GetLayCardsSameNumber(this List<Card> PlayerCards)
        {
            IDictionary<Card.CardNumber, List<Card>> CardsByNumber = new Dictionary<Card.CardNumber, List<Card>>();
            for (int i = 0; i < PlayerCards.Count; i++)
            {
                Card card = PlayerCards[i];
                if (CardsByNumber.ContainsKey(card.Number))
                    CardsByNumber[card.Number].Add(card);
                else
                    CardsByNumber.Add(card.Number, new List<Card> { card });
            }

            IDictionary<Card.CardNumber, List<Card>> LayCardsSameNumber = new Dictionary<Card.CardNumber, List<Card>>();

            foreach (KeyValuePair<Card.CardNumber, List<Card>> entry in CardsByNumber)
            {
                if (entry.Value.Count < 3)
                    continue;

                //The actual unique cards with the same number
                List<Card> uniqueCards = new List<Card>();
                foreach (Card card in entry.Value)
                {
                    if (!uniqueCards.Any(c => c.Symbol == card.Symbol))
                        uniqueCards.Add(card);
                }
                if (uniqueCards.Count >= 3)
                    LayCardsSameNumber.Add(entry.Key, uniqueCards);
            }
            return LayCardsSameNumber;
        }

        public static IDictionary<Card.CardNumber, List<Card>> GetLayCardsSeries(this List<Card> PlayerCards)
        {
            IDictionary<Card.CardNumber, List<Card>> LayCardsSeries = new Dictionary<Card.CardNumber, List<Card>>();
            //TODO
            Debug.LogWarning("GetLayCardsSeries not yet implemented");
            return LayCardsSeries;
        }
    }

}