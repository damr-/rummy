using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using romme.Cards;

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

        public static IDictionary<Card.CardRank, List<Card>> GetUniqueCardsByRank(this List<Card> PlayerCards)
        {
            IDictionary<Card.CardRank, List<Card>> uniqueCardsByRank = new Dictionary<Card.CardRank, List<Card>>();

            var cardsByRank = PlayerCards.GetCardsByRank();
            foreach (KeyValuePair<Card.CardRank, List<Card>> rank in cardsByRank)
            {
                var uniqueCards = rank.Value.GetUniqueCards();
                uniqueCardsByRank.Add(rank.Key, uniqueCards);
            }

            return uniqueCardsByRank;
        }

        public static IDictionary<Card.CardRank, List<Card>> GetCardsByRank(this List<Card> Cards)
        {
            var cardsByRank = new Dictionary<Card.CardRank, List<Card>>();
            for (int i = 0; i < Cards.Count; i++)
            {
                Card card = Cards[i];
                if (cardsByRank.ContainsKey(card.Rank))
                    cardsByRank[card.Rank].Add(card);
                else
                    cardsByRank.Add(card.Rank, new List<Card> { card });
            }
            return cardsByRank;
        }

        /// <summary>
        /// Takes a list of cards with the same rank and returns a new list of cards 
        /// which only contains a maximum of one card per occuring suit.
        /// </summary>
        /// <param name="sameRankCards">A list of cards of the same rank.</param>
        private static List<Card> GetUniqueCards(this List<Card> sameRankCards)
        {
            List<Card> uniqueCards = new List<Card>();

            if (sameRankCards.Count == 0)
                return uniqueCards;

            //Check if all ranks are the same
            Card first = sameRankCards[0];
            if (sameRankCards.Any(c => c.Rank != first.Rank))
            {
                Debug.LogWarning("GetUniqueCards() was passed a list of cards with more than one rank!");
                return uniqueCards;
            }

            foreach (Card card in sameRankCards)
            {
                if (!uniqueCards.Any(c => c.Suit == card.Suit))
                    uniqueCards.Add(card);
            }
            return uniqueCards;
        }

        /// <summary>
        /// Returns whether the list intersects the other list of cards.
        /// (Returns whether the two lists share one or more card(s))
        /// </summary>
        public static bool Intersects(this List<Card> list, List<Card> otherList)
        {
            foreach (var c1 in list)
            {
                foreach (var c2 in otherList)
                {
                    if (c1 == c2)
                        return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Returns the first card of the given list of cards which is not a joker, null otherwise
        /// </summary>
        public static Card GetFirstCard(this List<Card> cards)
        {
            foreach(var card in cards)
            {
                if(!card.IsJoker())
                    return card;
            }
            return null;
        }
    }
    
}