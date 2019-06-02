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
                            if (Cards.Count == 1) //The joker is the first and only card in the run, abort.
                                break;
                            if (Cards[1].Rank == Card.CardRank.TWO) //JOKER is ACE, ACE counts 1 in ACE-2-3
                                value += 1;
                            else //Find the next highest card which is not a joker and calculate the current joker's rank/value
                            {
                                var nonJokerIdx = CardUtil.GetFirstHigherNonJokerCardIdx(Cards, 1);
                                var jokerRank = Cards[nonJokerIdx].Rank - nonJokerIdx;
                                value += Card.CardValues[jokerRank];
                            }
                        }
                        else
                        {
                            //Try to find the first card below the current joker which is not one
                            var nonJokerIdx = CardUtil.GetFirstLowerNonJokerCardIdx(Cards, i - 1);
                            if (nonJokerIdx != -1)
                            {
                                Card.CardRank jokerRank = Cards[nonJokerIdx].Rank + (i - nonJokerIdx);
                                if (Cards[nonJokerIdx].Rank == Card.CardRank.ACE) // <=> nonJokerIdx is 0 <=> Cards[0].Rank is 1
                                    jokerRank = (Card.CardRank)(i + 1);

                                value += Card.CardValues[jokerRank];
                            }
                            else //No card below was found, search for higher card
                            {
                                nonJokerIdx = CardUtil.GetFirstHigherNonJokerCardIdx(Cards, i + 1);
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
                return value;
            }
        }

        public Run(List<Card> cards)
        {
            if (!CardUtil.IsValidRun(cards))
            {
                string output = "";
                cards.ForEach(card => output += card + ", ");
                Debug.LogWarning("The cards in a run are not in order or don't form a run! (" + output.TrimEnd().TrimEnd(',') + ")");
                return;
            }

            Cards = new List<Card>(cards);
            Suit = Cards.GetFirstCard().Suit;
        }

        public Card.CardRank GetHighestRank()
        {
            for (int i = Cards.Count - 1; i >= 0; i--)
            {
                if (!Cards[i].IsJoker())
                    return Cards[i].Rank + (Cards.Count - 1 - i);
            }
            Debug.LogWarning(ToString() + " only consists of jokers");
            return Card.CardRank.JOKER;
        }

        public Card.CardRank GetLowestRank()
        {
            for (int i = 0; i < Cards.Count; i++)
            {
                if (!Cards[i].IsJoker())
                    return Cards[i].Rank - i;
            }
            Debug.LogWarning(ToString() + " only consists of jokers");
            return Card.CardRank.JOKER;
        }

        public Card.CardColor GetRunColor()
        {
            if (Suit == Card.CardSuit.CLOVERS || Suit == Card.CardSuit.PIKE)
                return Card.CardColor.BLACK;
            return Card.CardColor.RED;
        }

        ///<summary>
        ///Returns whether the cards of the other run LOOK the same as the cards in this
        ///meaning that they are not change for object-equality but only for same rank 
        ///</summary>
        public bool LooksEqual(Run other)
        {
            if (Count != other.Count)
                return false;
            if (Suit != other.Suit)
                return false;

            for (int i = 0; i < Cards.Count(); i++)
            {
                Card.CardRank r1 = Cards.ElementAt(i).Rank;
                Card.CardRank r2 = other.Cards.ElementAt(i).Rank;

                if (r1 != r2)
                    return false;
            }
            return true;
        }

    }

}