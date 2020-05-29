using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using rummy.Utility;

namespace rummy.Cards
{
    public class Set : Pack
    {
        public Card.CardRank Rank { get; private set; }
        public int Value => Cards.Count * Cards.GetFirstCard().Value;

        public Set(Card c1, Card c2, Card c3) : this(new List<Card>() { c1, c2, c3 }) { }
        public Set(List<Card> cards)
        {
            if (!CardUtil.IsValidSet(cards))
            {
                Debug.LogWarning("The cards in a set are not all of the same rank or some share the same suit!");
                return;
            }

            Cards = new List<Card>() {};
            Cards.AddRange(cards);
            Rank = Cards.GetFirstCard().Rank;
        }
        
        public bool HasTwoBlackCards() => Cards.Count(c => c.IsBlack()) == 2;
        public bool HasTwoRedCards() => Cards.Count(c => c.IsRed()) == 2;

        ///<summary>
        ///Returns whether the cards of the other set LOOK the same as the cards in this
        ///meaning that they are not change for object-equality but only for same suit 
        ///</summary>
        public bool LooksEqual(Set other)
        {
            if(Count != other.Count)
                return false;
            if(Rank != other.Rank)
                return false;
            
            var ordered1 = Cards.OrderBy(c => c.Suit);
            var ordered2 = other.Cards.OrderBy(c => c.Suit);

            for(int i = 0; i < ordered1.Count(); i++)
            {
                Card.CardSuit s1 = ordered1.ElementAt(i).Suit;
                Card.CardSuit s2 = ordered2.ElementAt(i).Suit;

                if(s1 != s2)
                    return false;
            }
            return true;
        }
    }

}