using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using rummy.Cards;

namespace rummy.Utility
{
    public static class Extensions
    {
        public static int Seed;

        public static Stack<T> Shuffle<T>(this Stack<T> stack)
        {
            Random.InitState(Seed);
            return new Stack<T>(stack.OrderBy(x => Random.Range(0, int.MaxValue)));
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
        /// Returns whether the list intersects the other list of cards - whether the two lists have (at least) one card in common
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