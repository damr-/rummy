using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using rummy.Utility;

namespace rummy.Cards
{

    public class Run : Pack
    {
        public Card.CardSuit Suit { get; private set; }
        public Card.CardColor Color { get; private set; }
        public Card.CardRank HighestRank { get; private set; }
        public Card.CardRank LowestRank { get; private set; }

        public Run(List<Card> cards)
        {
            if (!CardUtil.IsValidRun(cards))
            {
                string output = "";
                cards.ForEach(card => output += card + ", ");
                Tb.I.GameMaster.LogMsg("The cards in a run are not in order or don't form a run! (" + output.TrimEnd().TrimEnd(',') + ")", LogType.Error);
                return;
            }

            Cards = new List<Card>(cards);
            Card firstCard = Cards.GetFirstCard();
            Suit = firstCard.Suit;
            Color = firstCard.Color;
            CalculateValue();
            HighestRank = CalculateRankExtremum(Cards.Count - 1, false);
            LowestRank = CalculateRankExtremum(0, true);
        }

        private Card.CardRank CalculateRankExtremum(int startIndex, bool searchForward)
        {
            int idx = CardUtil.GetFirstNonJokerCardIdx(Cards, startIndex, searchForward);
            if (idx == -1)
            {
                Tb.I.GameMaster.LogMsg(ToString() + " only consists of jokers", LogType.Error);
                return Card.CardRank.JOKER;
            }
            if (searchForward)
                return Cards[idx].Rank - idx;
            else
                return Cards[idx].Rank + (Cards.Count - 1 - idx);
        }

        ///<summary>
        ///Returns whether the cards of the other run LOOK the same as the cards in this
        ///meaning that they are not checked for object-equality but only for same rank 
        ///</summary>
        public bool LooksEqual(Run other)
        {
            if (Count != other.Count)
                return false;
            if (Suit != other.Suit)
                return false;
            if (Value != other.Value)
                return false;

            for (int i = 0; i < Cards.Count(); i++)
            {
                if (Cards[i].Rank != other.Cards[i].Rank)
                    return false;
            }
            return true;
        }

        private void CalculateValue()
        {
            int value = 0;

            for (int i = 0; i < Count; i++)
            {
                var rank = Cards[i].Rank;
                if (rank == Card.CardRank.JOKER)
                {
                    if (i == 0)
                    {
                        if (Cards.Count == 1) //The joker is the first and only card in the run, abort.
                            break;
                        if (Cards[1].Rank == Card.CardRank.TWO) //JOKER is ACE, ACE counts 1 in ACE-2-3
                            value += 1;
                        else //Find the next highest card which is not a joker and calculate the current joker's rank&value
                        {
                            var nonJokerIdx = CardUtil.GetFirstNonJokerCardIdx(Cards, 1, true);
                            var jokerRank = Cards[nonJokerIdx].Rank - nonJokerIdx;
                            value += Card.CardValues[jokerRank];
                        }
                    }
                    else
                    {
                        //Try to find the first non-joker card below the current joker
                        var nonJokerIdx = CardUtil.GetFirstNonJokerCardIdx(Cards, i - 1, false);
                        if (nonJokerIdx != -1)
                        {
                            Card.CardRank jokerRank = Cards[nonJokerIdx].Rank + (i - nonJokerIdx);
                            if (Cards[nonJokerIdx].Rank == Card.CardRank.ACE) // <=> nonJokerIdx is 0 <=> Cards[0].Rank is 1
                                jokerRank = (Card.CardRank)(i + 1);
                            value += Card.CardValues[jokerRank];
                        }
                        else //No card below was found, search for higher card
                        {
                            nonJokerIdx = CardUtil.GetFirstNonJokerCardIdx(Cards, i + 1, true);
                            if (nonJokerIdx != -1)
                            {
                                var jokerRank = Cards[nonJokerIdx].Rank - (nonJokerIdx - i);
                                value += Card.CardValues[jokerRank];
                            }
                        }
                    }
                }
                else if (rank == Card.CardRank.ACE && i == 0)
                {
                    value += 1; //ACE counts 1 in ACE-2-3
                }
                else
                {
                    value += Cards[i].Value;
                }
            }
            Value = value;
        }
    }

}