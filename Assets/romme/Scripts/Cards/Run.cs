using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using romme.Utility;

namespace romme.Cards
{
    public class Run : Pack
    {
        public Card.CardSuit Suit { get; private set; }
        public int Value
        {
            get
            {
                int value = 0;

                for (int i = 0; i < Count; i++)
                {
                    var rank = Cards[i].Rank;
                    if (rank == Card.CardRank.JOKER)
                    {
                        if (i == 0)
                        {
                            if (Cards[1].Rank == Card.CardRank.TWO)
                                value += 1; //JOKER is ACE, ACE counts 1 in ACE-2-3
                            else
                                value += Card.CardValues[Cards[1].Rank - 1];
                        }
                        else
                        {
                            value += Card.CardValues[Cards[i - 1].Rank + 1];
                        }
                    }
                    else if(rank == Card.CardRank.ACE && i == 0)
                    {
                        value += 1; //ACE counts 1 in ACE-2-3
                    }
                    else
                    {
                        value += Cards[i].Value;
                    }
                }
                return value;
            }
        }

        public Run(List<Card> cards)
        {
            if (!CardUtil.IsValidRun(cards))
            {
                string output = "";
                cards.ForEach(card => output += card + ", ");
                Debug.LogError("The cards in a run are not in order or don't form a run! (" + output.TrimEnd().TrimEnd(',') + ")");
                return;
            }

            Cards = new List<Card>();
            Cards.AddRange(cards);
            Suit = Cards.GetFirstCard().Suit;
        }

        public Card.CardRank GetHighestRank()
        {
            var lastCardRank = Cards[Cards.Count - 1].Rank;
            return lastCardRank != Card.CardRank.JOKER ? lastCardRank : Cards[Cards.Count - 2].Rank + 1;
        }

        public Card.CardRank GetLowestRank()
        {
            var firstCardRank = Cards[0].Rank;
            return firstCardRank != Card.CardRank.JOKER ? firstCardRank : Cards[1].Rank - 1;
        }

        public Card.CardColor GetRunColor()
        {
            if(Suit == Card.CardSuit.CLOVERS || Suit == Card.CardSuit.PIKE)
                return Card.CardColor.BLACK;
            return Card.CardColor.RED;
        }

        ///<summary>
        ///Returns whether the cards of the other run LOOK the same as the cards in this
        ///meaning that they are not change for object-equality but only for same rank 
        ///</summary>
        public bool LooksEqual(Run other)
        {
            if(Count != other.Count)
                return false;
            if(Suit != other.Suit)
                return false;

            for(int i = 0; i < Cards.Count(); i++)
            {
                Card.CardRank r1 = Cards.ElementAt(i).Rank;
                Card.CardRank r2 = other.Cards.ElementAt(i).Rank;

                if(r1 != r2)
                    return false;
            }
            return true;
        }

    }

}