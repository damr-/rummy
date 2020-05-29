using System.Linq;
using System.Collections.Generic;
using UnityEngine;
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
            if (!CardUtil.IsValidSet(cards))
            {
                Tb.I.GameMaster.LogMsg("The cards in a set are not all of the same rank or some share the same suit!", LogType.Error);
                return;
            }

            Cards = new List<Card>(cards);
            Rank = Cards.GetFirstCard().Rank;
            Value = Cards.Count * Cards.GetFirstCard().Value;
            HasTwoBlackCards = Cards.Count(c => c.IsBlack()) == 2;
            HasTwoRedCards = Cards.Count(c => c.IsRed()) == 2;
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