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

        public static IDictionary<Card.CardNumber, List<Card>> GetCardsByNumber(this List<Card> PlayerCards)
        {
            var cards = new Dictionary<Card.CardNumber, List<Card>>();
            for (int i = 0; i < PlayerCards.Count; i++)
            {
                Card card = PlayerCards[i];
                if (cards.ContainsKey(card.Number))
                    cards[card.Number].Add(card);
                else
                    cards.Add(card.Number, new List<Card> { card });
            }
            return cards;
        }

        public static IDictionary<Card.CardNumber, List<Card>> GetUniqueCardsByNumber(this List<Card> PlayerCards)
        {
            IDictionary<Card.CardNumber, List<Card>> uniqueCardsByNumber = new Dictionary<Card.CardNumber, List<Card>>();

            var CardsByNumber = PlayerCards.GetCardsByNumber();
            foreach (KeyValuePair<Card.CardNumber, List<Card>> entry in CardsByNumber)
            {
                var uniqueCards = entry.Value.GetUniqueCards();
                uniqueCardsByNumber.Add(entry.Key, uniqueCards);
            }

            return uniqueCardsByNumber;
        }

        public static List<Card> GetUniqueCards(this List<Card> Cards)
        {
            List<Card> uniqueCards = new List<Card>();
            foreach (Card card in Cards)
            {
                if (!uniqueCards.Any(c => c.Symbol == card.Symbol))
                    uniqueCards.Add(card);
            }
            return uniqueCards;
        }

        public static List<KeyValuePair<Card.CardNumber, List<Card>>> GetLayCardsSameNumber(this List<Card> PlayerCards)
        {
            //var CardsByNumber = PlayerCards.GetCardsByNumber();
            //var LayCardsSameNumber = new Dictionary<Card.CardNumber, List<Card>>();

            //foreach (KeyValuePair<Card.CardNumber, List<Card>> entry in CardsByNumber)
            //{
            //    if (entry.Value.Count < 3)
            //        continue;

            //    //The actual unique cards with the same number
            //    List<Card> uniqueCards = new List<Card>();
            //    foreach (Card card in entry.Value)
            //    {
            //        if (!uniqueCards.Any(c => c.Symbol == card.Symbol))
            //            uniqueCards.Add(card);
            //    }
            //    if (uniqueCards.Count >= 3)
            //        LayCardsSameNumber.Add(entry.Key, uniqueCards);
            //}
            ////return LayCardsSameNumber;
            return PlayerCards.GetUniqueCardsByNumber().Where(entry => entry.Value.Count >= 3).ToList();
        }

        public static IDictionary<Card.CardNumber, List<Card>> GetLayCardsSeries(this List<Card> PlayerCards)
        {
            var LayCardsSeries = new Dictionary<Card.CardNumber, List<Card>>();
            //TODO
            Debug.LogWarning("GetLayCardsSeries not yet implemented");
            return LayCardsSeries;
        }
    }

}