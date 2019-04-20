using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace romme.Cards
{
    public class Run
    {
        public Card.CardSuit Suit;
        public List<Card> Cards {get; private set;}
        public int Count => Cards.Count;
        public int Value{
            get {
                int value = 0;

                for(int i = 0; i < Count; i++)
                {
                    var rank = Cards[i].Rank;
                    if(rank == Card.CardRank.JOKER)
                    {
                        if(i == 0)
                        {
                            if(Cards[1].Rank == Card.CardRank.TWO)
                                value += Card.CardValues[Card.CardRank.ACE];
                            else
                                value += Card.CardValues[Cards[1].Rank-1];
                        }
                        else 
                        {
                            value += Card.CardValues[Cards[i-1].Rank+1];
                        }
                    }
                    else
                        value += Cards[i].Value();
                }

                return value;
            }
        }

        public Run(Card c1, Card c2, Card c3)
        {
            Cards = new List<Card>(){c1, c2, c3};
            Suit = c1.Suit;
            CheckValidity();
        }

        public Run(List<Card> cards)
        {
            Cards = new List<Card>();
            Cards.AddRange(cards);
            if(Cards.Count > 0)
                Suit = Cards[0].Suit;
            CheckValidity();
        }

        private void CheckValidity()
        {
            if(!IsValidRun())
                Debug.LogWarning("The cards in a run are not in order or don't form a run!");
        }

        private bool IsValidRun()
        {
            if(Cards.Count == 0)
                return false;

            //A run can only consist of cards with the same suit
            if(Cards.Any(c => c.Suit != Cards[0].Suit))
                return false;

            for(int i = 0; i < Count-1; i++)
            {
                //Ace can be the start of a run
                if(i == 0 && Cards[i].Rank == Card.CardRank.ACE)
                {
                    //Run is only valid if next card is a TWO or a JOKER
                    if(Cards[i+1].Rank != Card.CardRank.TWO && Cards[i+1].Rank != Card.CardRank.JOKER)
                       return false;
                }//otherwise, rank has to increase by one
                else if(Cards[i+1].Rank != Cards[i].Rank+1 && Cards[i+1].Rank != Card.CardRank.JOKER)
                    return false;
            }
            return true;
        }

        public bool Equal(Run other)
        {
            if(Count != other.Count)
                return false;

            for(int i = 0; i < Count; i++)
            {
                for(int j = 0; j < other.Count; j++)
                {
                    if(Cards[i] != other.Cards[j])
                        return false;
                }
            }
            
            return true;   
        }

        public bool Intersects(Run other) => Intersects(other.Cards);

        private bool Intersects(List<Card> cards)
        {
            foreach(var c1 in Cards)
            {
                foreach(var c2 in cards)
                {
                    if(c1 == c2)
                        return true;
                }
            }
            return false;
        }

        public bool Contains(Run other)
        {
            if(other.Count > Count)
                return false;
            if(Equal(other))
                return true;
            
            //Find the index of the element in this run which matches the first element of the 'other' run
            int firstIndex = -1;
            for(int i = 0; i < Count; i++)
            {
                if(Cards[i] == other.Cards[0])
                    firstIndex = i;
            }
            if(firstIndex == -1)
                return false;

            //Check if the number of remaining elements is too little to possibly equal the 'other' run
            if(firstIndex + other.Count > Count)
                return false;

            //Create a new run of the subset and check if each element is identical to the 'other' run
            List<Card> subset = new List<Card>();
            subset.AddRange(Cards.GetRange(firstIndex, other.Count));
            Run subsetRun = new Run(subset);            
            return subset.Equals(other);
        }

        /// <summary>
        /// Check whether the any card in this run is the same as any in the given set
        /// </summary>
        public bool Intersects(Set set)
        {
            //First find all the cards which share the rank
            var rank = set.Cards[0].Rank;
            var matches = Cards.Where(c => c.Rank == rank).ToList();
            if(matches.Count == 0)
                return false;

            //Check if any of the found cards are actually also part of the set
            return Intersects(matches);
        }

        public override string ToString()
        {
            string output = "";
            foreach(var card in Cards)
                output += card + " ";
            return output.TrimEnd();
        }
    }

}