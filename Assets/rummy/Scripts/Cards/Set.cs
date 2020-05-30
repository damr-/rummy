using System.Linq;
using System.Collections.Generic;
using rummy.Utility;

namespace rummy.Cards
{

    public class Set : Pack
    {
        public Card.CardRank Rank { get; private set; }
        public bool HasTwoBlackCards { get; private set; }
        public bool HasTwoRedCards { get; private set; }

        public Set(Card c1, Card c2, Card c3) : this(new List<Card>() { c1, c2, c3 }) { }
        public Set(List<Card> cards)
        {
            if (!IsValidSet(cards))
            {
                string msg = "Invalid set: ";
                cards.ForEach(card => msg += msg + ", ");
                throw new RummyException(msg.TrimEnd().TrimEnd(','));
            }

            Cards = new List<Card>(cards);
            Rank = Cards.GetFirstCard().Rank;
            Value = Cards.Count * Cards.GetFirstCard().Value;
            HasTwoBlackCards = Cards.Count(c => c.IsBlack()) == 2;
            HasTwoRedCards = Cards.Count(c => c.IsRed()) == 2;
        }

        /// <summary>
        /// Returns whether the given list of cards could form a valid set
        /// </summary>
        public static bool IsValidSet(List<Card> cards)
        {
            if (cards.Count < 3 || cards.Count > 4)
                return false;

            //A set can only consist of cards with the same rank and/or a joker
            if (cards.Any(c => !c.IsJoker() && c.Rank != cards.GetFirstCard().Rank))
                return false;

            var usedSuits = new List<Card.CardSuit>();
            for (int i = 0; i < cards.Count; i++)
            {
                if (cards[i].IsJoker())
                    continue; //Skip checking joker for now

                var suit = cards[i].Suit;
                if (usedSuits.Contains(suit))
                    return false;
                usedSuits.Add(suit);
            }

            //Check joker now if necessary
            Card joker = cards.FirstOrDefault(c => c.IsJoker());
            if (joker != null)
            {
                if (joker.IsBlack()
                    && usedSuits.Contains(Card.CardSuit.CLUBS)
                    && usedSuits.Contains(Card.CardSuit.SPADES))
                    return false;
                if (joker.IsRed()
                    && usedSuits.Contains(Card.CardSuit.HEARTS)
                    && usedSuits.Contains(Card.CardSuit.DIAMONDS))
                    return false;
            }
            return true;
        }

        ///<summary>
        /// Returns whether the cards of the other set LOOK the same as the cards in this
        /// meaning that they are not checked for object-equality but only for same suit 
        ///</summary>
        public bool LooksEqual(Set other)
        {
            if (Count != other.Count)
                return false;
            if (Rank != other.Rank)
                return false;

            var o1 = Cards.OrderBy(c => c.Suit);
            var o2 = other.Cards.OrderBy(c => c.Suit);

            for (int i = 0; i < o1.Count(); i++)
            {
                if (o1.ElementAt(i).Suit != o2.ElementAt(i).Suit)
                    return false;
            }
            return true;
        }
    }

}