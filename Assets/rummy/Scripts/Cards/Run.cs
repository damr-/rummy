using System.Linq;
using System.Collections.Generic;
using rummy.Utility;

namespace rummy.Cards
{

    public class Run : Meld
    {
        public Card.CardSuit Suit { get; private set; }
        public Card.CardColor Color { get; private set; }
        public Card.CardRank HighestRank { get; private set; }
        public Card.CardRank LowestRank { get; private set; }

        public Run(Card c1, Card c2, Card c3) : this(new List<Card>() { c1, c2, c3 }) { }
        public Run(List<Card> cards)
        {
            if (!IsValidRun(cards))
            {
                string msg = "";
                cards.ForEach(card => msg += card + ", ");
                throw new RummyException("Invalid run: " + msg.TrimEnd().TrimEnd(','));
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
            int idx = Cards.GetFirstCardIndex(startIndex, searchForward);
            if (idx == -1)
                throw new RummyException(ToString() + " only consists of jokers");
            if (searchForward)
                return Cards[idx].Rank - idx;
            else
                return Cards[idx].Rank + (Cards.Count - 1 - idx);
        }

        /// <summary>
        /// Returns whether the cards of the other run LOOK the same as the cards in this
        /// meaning that they are not checked for object-equality but only for same rank 
        /// </summary>
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
                    if (i == 0 && Cards.Count == 1)
                    {
                        // The joker is the first and only card in the run,
                        // which will have value zero for now
                        break;
                    }

                    var jokerRank = CardUtil.GetJokerRank(Cards, i);

                    // If the next card is a TWO, jokerRank is RANK(2-1)=Rank(1)=JOKER
                    if (jokerRank == Card.CardRank.JOKER)
                    {
                        // JOKER is ACE, ACE counts 1 in ACE-2-3
                        value += 1;
                    }
                    else
                    {
                        value += Card.CardValues[jokerRank];
                    }
                }
                else if (rank == Card.CardRank.ACE && i == 0)
                {
                    // ACE counts 1 in ACE-2-3
                    value += 1;
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

            // A run can only consist of cards with the same suit (or joker with the matching color)
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
                // Ace can be the start of a run
                if (i == 0 && cards[i].Rank == Card.CardRank.ACE)
                {
                    // Run is only valid if next card is a TWO or a JOKER
                    if (cards[i + 1].Rank != Card.CardRank.TWO && !cards[i + 1].IsJoker())
                        return false;
                }
                // otherwise, rank has to increase by one
                else if (cards[i + 1].Rank != cards[i].Rank + 1 && !cards[i].IsJoker() && !cards[i + 1].IsJoker())
                    return false;
            }
            return true;
        }

        public bool CanFit(Card card, out Card Joker)
        {
            Joker = null;

            if (card.IsJoker())
                return card.Color == Color && Cards.Count < 14;

            var jokers = Cards.Where(c => c.IsJoker());

            if (card.Suit != Suit || (Cards.Count == 14 && !jokers.Any()))
                return false;

            // Check whether the new card replaces a joker
            foreach (var joker in jokers)
            {
                var jokerRank = CardUtil.GetJokerRank(Cards, Cards.IndexOf(joker));
                if (jokerRank == card.Rank)
                {
                    Joker = joker;
                    return true;
                }
            }

            return (HighestRank != Card.CardRank.ACE && card.Rank == HighestRank + 1) ||
                    (LowestRank != Card.CardRank.ACE && card.Rank == LowestRank - 1) ||
                    (LowestRank == Card.CardRank.TWO && card.Rank == Card.CardRank.ACE);
        }

        /// <summary>
        /// Returns the possible values a joker could have when added to the start/end of this run
        /// </summary>
        /// <returns>A tuple with the possible values for start and end. If a value is 0, a joker cannot be added there</returns>
        public (int,int) JokerValue()
        {
            int i1 = 0;
            if (LowestRank != Card.CardRank.ACE)
                i1 = (LowestRank == Card.CardRank.TWO) ? 1 : (int)(LowestRank - 1);
            int i2 = 0;
            if (HighestRank!= Card.CardRank.ACE)
                i2 = (int)(HighestRank + 1);
            return (i1, i2);
        }

    }

}