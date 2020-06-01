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
        /// Returns the given list of cards as a dictionary. Keys are CardSuits, Values are all the cards in 'cards' with that suit.
        /// </summary>
        public static IDictionary<Card.CardSuit, List<Card>> GetCardsBySuit(this List<Card> cards)
        {
            var cardsBySuit = new Dictionary<Card.CardSuit, List<Card>>();
            for (int i = 0; i < cards.Count; i++)
            {
                Card card = cards[i];
                if (cardsBySuit.ContainsKey(card.Suit))
                    cardsBySuit[card.Suit].Add(card);
                else
                    cardsBySuit.Add(card.Suit, new List<Card> { card });
            }
            return cardsBySuit;
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
        /// Returns the first card of the given list of cards which is not a joker
        /// </summary>
        /// <returns>The found card or null otherwise</returns>
        public static Card GetFirstCard(this List<Card> cards)
        {
            int idx = GetFirstCardIndex(cards, 0, true);
            return idx > -1 ? cards[idx] : null;
        }

        /// <summary>
        /// Returns the index of the first card which is not a joker, starting the search at startIndex and searching in the desired direction
        /// </summary>
        /// <returns>The index of the first non-joker card or -1 if none was found</returns>
        public static int GetFirstCardIndex(this List<Card> cards, int startIndex = 0, bool searchForward = true)
        {
            int increment = searchForward ? +1 : -1;
            for (int i = startIndex; i < cards.Count && i >= 0; i += increment)
            {
                if (!cards[i].IsJoker())
                    return i;
            }
            return -1;
        }

        /// <summary>
        /// Returns each .ToString() of a list of MonoBehaviours concatenated to string and prefixed with 'title'
        /// The passed list type T has to implement .ToString() !
        /// </summary>
        /// <param name="list">The list of MonoBehaviours</param>
        /// <param name="title">The title of a single monobehaviour. An 's' will be appended if list.Count > 0</param>
        public static string GetListMsg<T>(this List<T> list, string title)
        {
            string msg = "";
            list.ForEach(duo => msg += duo.ToString() + ", ");
            return title + (list.Count > 1 ? "s " : " ") + msg.TrimEnd().TrimEnd(',');
        }
    }

}