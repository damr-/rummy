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

        public static List<KeyValuePair<Card.CardRank, List<Card>>> GetSets(this List<Card> PlayerCards)
        {
            return PlayerCards.GetUniqueCardsByRank().Where(entry => entry.Value.Count >= 3).ToList();
        }

        public static IDictionary<Card.CardRank, List<Card>> GetUniqueCardsByRank(this List<Card> PlayerCards)
        {
            IDictionary<Card.CardRank, List<Card>> uniqueCardsByRank = new Dictionary<Card.CardRank, List<Card>>();

            var cardsByRank = PlayerCards.GetCardsByRank();
            foreach (KeyValuePair<Card.CardRank, List<Card>> cards in cardsByRank)
            {
                var uniqueCards = cards.Value.GetUniqueCards();
                uniqueCardsByRank.Add(cards.Key, uniqueCards);
            }

            return uniqueCardsByRank;
        }

        public static IDictionary<Card.CardRank, List<Card>> GetCardsByRank(this List<Card> PlayerCards)
        {
            var cards = new Dictionary<Card.CardRank, List<Card>>();
            for (int i = 0; i < PlayerCards.Count; i++)
            {
                Card card = PlayerCards[i];
                if (cards.ContainsKey(card.Rank))
                    cards[card.Rank].Add(card);
                else
                    cards.Add(card.Rank, new List<Card> { card });
            }
            return cards;
        }

        /// <summary>
        /// Takes a list of cards with the same rank and returns a new list of cards 
        /// which only contains a maximum of one card per occuring suit.
        /// </summary>
        /// <param name="sameRankCards">A list of cards of the same rank.</param>
        public static List<Card> GetUniqueCards(this List<Card> sameRankCards)
        {
            List<Card> uniqueCards = new List<Card>();
            
            if(sameRankCards.Count == 0)
                return uniqueCards;

            Card first = sameRankCards[0];
            if(sameRankCards.Any(c => c.Rank != first.Rank))
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
        /// Returns the first card in PlayerCards which has the same suit and is one rank higher or lower compared to the given card
        /// </summary>
        /// <returns> The card or null if none was found </returns>
        public static Card GetCardOneRankDiff(List<Card> PlayerCards, Card card, bool higher)
        {
            foreach(Card otherCard in PlayerCards)
            {
                if(otherCard == card || otherCard.Suit != card.Suit)
                    continue;           
                //TODO: allow going from ACE to TWO but only if ACE does not connect to KING too
                //if(card is first in run && card.Rank == Card.CardRank.ACE && otherCard.Rank == Card.CardRank.TWO)
                    //return otherCard;
                if(otherCard.Rank == card.Rank + (higher ? 1 : -1))
                    return otherCard;
            }
            return null;
        }

        public static List<List<Card>> GetRuns(this List<Card> PlayerCards)
        {
            var runs = new List<List<Card>>();

            foreach(Card card in PlayerCards)
            {
                Card neighbor = GetCardOneRankDiff(PlayerCards, card, true);
                List<Card> series = new List<Card> { card };

                while(neighbor != null)
                {
                    series.Add(neighbor);
                    neighbor = GetCardOneRankDiff(PlayerCards, neighbor, true);
                }

                if(series.Count >= 3)
                    runs.Add(series);
            }
            return runs;
        }

        ///<summary>Return all the possible sets which can be formed using joker cards ordered descending by the value of the set.
        ///The passed 'CompletePossibleSets' should consist of all the sets which will be laid down anyway since they are complete.</summary>
        public static List<KeyValuePair<Card.CardColor, KeyValuePair<Card.CardRank, List<Card>>>> 
            GetJokerSets(this List<Card> PlayerCards, List<KeyValuePair<Card.CardRank, List<Card>>> completePossibleSets)
        {
            List<Card> jokerCards = PlayerCards.Where(c => c.Rank == Card.CardRank.JOKER).ToList();
            int jokerCount = jokerCards.Count;
            if (jokerCount == 0)
                return new List<KeyValuePair<Card.CardColor, KeyValuePair<Card.CardRank, List<Card>>>>();

            //Get all possible sets with more than one card so that 1 joker can finish a trio
            var possibleCardsUnfiltered = PlayerCards.GetCardsByRank().Where(entry => entry.Key != Card.CardRank.JOKER &&
                                                                                entry.Value.Count >= 2);

            var possibleJokerSets = new List<KeyValuePair<Card.CardRank, List<Card>>>();
            foreach(var entry in possibleCardsUnfiltered)
            {
                //Get all cards which will be laid down anyway and which have the same CardNumber as the currently investigated cards
                var layCardsWithSameCardNumber = completePossibleSets.Where(e => e.Key == entry.Key);

                List<Card> eligibleCards = new List<Card>();

                //No sets were gonna be laid down, so all the possible cards are eligible
                if (!layCardsWithSameCardNumber.Any())
                {
                    eligibleCards = entry.Value;
                }
                else
                {
                    foreach (var sameCardNumberPair in layCardsWithSameCardNumber)
                    {
                        foreach (Card card in entry.Value)
                        {
                            //If card is not gonna be laid down, it is eligible for being used with a joker
                            if (!sameCardNumberPair.Value.Contains(card))
                                eligibleCards.Add(card);
                        }
                    }
                }

                if(eligibleCards.Count == 0)
                    continue;

                var uniqueEligibleCards = eligibleCards.GetUniqueCards();
                if (uniqueEligibleCards.Count == 2)
                    possibleJokerSets.Add(new KeyValuePair<Card.CardRank, List<Card>>(entry.Key, uniqueEligibleCards));
            }

            if (!possibleJokerSets.Any())
                return new List<KeyValuePair<Card.CardColor, KeyValuePair<Card.CardRank, List<Card>>>>();

            var coloredPossibleJokerSets = new List<KeyValuePair<Card.CardColor, KeyValuePair<Card.CardRank, List<Card>>>>();
            //Select possible card combinations by required joker color
            for (int i = 0; i < 2; i++)
            {
                Card.CardColor curColor = (Card.CardColor)i;
                
                //Skip current color if no joker has this color
                if(!jokerCards.Any(jc => jc.Color == curColor))
                    continue;

                foreach (var possibleJokerSet in possibleJokerSets)
                {
                    //Adding a joker to a set is only possible if it differs in color compared to the other two cards
                    if (possibleJokerSet.Value.Count(c => c.Color == curColor) != 2)
                        coloredPossibleJokerSets.Add(new KeyValuePair<Card.CardColor, KeyValuePair<Card.CardRank, List<Card>>>(curColor, possibleJokerSet));
                }
            }
            coloredPossibleJokerSets = coloredPossibleJokerSets.OrderByDescending(entry => (int)entry.Value.Key).ToList();
            return coloredPossibleJokerSets;
        }

        // public static bool Identical(this List<Card> set1, List<Card> set2)
        // {
        //    if(set1.Count != set2.Count)
        //        return false;

        //    foreach(Card c1 in set1)
        //    {
        //        foreach(Card c2 in set2)
        //        {
        //            if(c1 != c2)
        //                return false;
        //        }
        //    }
        //    return true;
        // }
    }

}