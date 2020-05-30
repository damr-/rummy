using System.Linq;
using System.Collections.Generic;
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
            if (!IsValidRun(cards))
            {
                string msg = "";
                cards.ForEach(card => msg += msg + ", ");
                msg = "Invalid run: " + msg.TrimEnd().TrimEnd(',');
                throw new RummyException(msg);
            }

            Cards = new List<Card>(cards);
            Card firstCard = Cards.GetFirstCard();
            Suit = firstCard.Suit;
            Color = firstCard.Color;
            CalculateValue();
            HighestRank = GetRankExtremum(true);
            LowestRank = GetRankExtremum(false);
        }

        /// <summary>
        /// Calculates and returns the maximum/minimum Rank in this run, depending on
        /// </summary>
        /// <param name="maxRank">Whether to look for the highest rank. If false, the lowest rank is returned</param>
        /// <returns></returns>
        private Card.CardRank GetRankExtremum(bool maxRank)
        {
            int startIndex = maxRank ? Cards.Count - 1 : 0;
            bool searchForward = !maxRank;
            int idx = CardUtil.GetFirstNonJokerCardIdx(Cards, startIndex, searchForward);
            if (idx == -1)
                throw new RummyException(ToString() + " only consists of jokers");
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


        /// <summary>
        /// Returns whether the given list of cards could form a valid run
        /// </summary>
        public static bool IsValidRun(List<Card> cards)
        {
            if (cards.Count < 3)
                return false;

            //A run can only consist of cards with the same suit (or joker with the matching color)
            Card representiveCard = cards.GetFirstCard();
            if (representiveCard == null)
                return false;

            foreach (var card in cards)
            {
                if (!card.IsJoker())
                {
                    if (card.Suit != representiveCard.Suit)
                        return false;
                }
                else
                {
                    if (card.Color != representiveCard.Color)
                        return false;
                }
            }

            for (int i = 0; i < cards.Count - 1; i++)
            {
                //Ace can be the start of a run
                if (i == 0 && cards[i].Rank == Card.CardRank.ACE)
                {
                    //Run is only valid if next card is a TWO or a JOKER
                    if (cards[i + 1].Rank != Card.CardRank.TWO && !cards[i + 1].IsJoker())
                        return false;
                }
                //otherwise, rank has to increase by one
                else if (cards[i + 1].Rank != cards[i].Rank + 1 && !cards[i].IsJoker() && !cards[i + 1].IsJoker())
                    return false;
            }
            return true;
        }

    }

}