using System.Collections.Generic;
using rummy.Cards;

namespace rummy.Utility
{
    public static class Extensions
    {
        /// <summary>
        /// Returns the given list of cards as a dictionary. Keys are Card ranks, values are the cards from the list.
        /// </summary>
        public static IDictionary<Card.CardRank, List<Card>> GetCardsByRank(this List<Card> cards)
        {
            var cardsByRank = new Dictionary<Card.CardRank, List<Card>>();
            for (int i = 0; i < cards.Count; i++)
            {
                Card card = cards[i];
                if (cardsByRank.ContainsKey(card.Rank))
                    cardsByRank[card.Rank].Add(card);
                else
                    cardsByRank.Add(card.Rank, new List<Card> { card });
            }
            return cardsByRank;
        }
        
        /// <summary>
        /// Returns whether the list intersects the other list of cards (whether the two lists have at least one card in common)
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