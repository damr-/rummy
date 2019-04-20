using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace romme.Cards
{
    public class Set
    {
        public Card.CardRank Rank {get; private set; }
        public List<Card> Cards {get; private set;}
        public int Count=> Cards.Count;
        //If value of first card is 0, it's a joker. If so, just take the value of the second card
        public int Value => Cards.Count * (Cards[0].Value() != 0 ? Cards[0].Value() : Cards[1].Value());

        public Set(Card c1, Card c2, Card c3)
        {
            Cards = new List<Card>(){c1,c2,c3};
            Rank = c1.Rank;
            CheckValidity();
        }

        public Set(Card c1, Card c2, Card c3, Card c4)
        {
            Cards = new List<Card>(){c1,c2,c3,c4};
            Rank = c1.Rank;
            CheckValidity();
        }

        public Set(List<Card> cards)
        {
            Cards = new List<Card>(){};
            Cards.AddRange(cards);
            if(Cards.Count() > 0)
                Rank = Cards[0].Rank;
            CheckValidity();
        }

        private void CheckValidity()
        {
            if(!IsValidSet())
                Debug.LogWarning("The cards in a set are not all of the same rank or some share the same suit!");
        }

        private bool IsValidSet()
        {
            if(Cards.Count == 0)
                return false;
            //A set can only consist of cards with the same rank
            if(Cards.Any(c => c.Rank != Cards[0].Rank))
                return false;
            var usedSuits = new List<Card.CardSuit>();
            for(int i = 0; i < Count; i++)
            {
                var suit = Cards[i].Suit;
                if(usedSuits.Contains(suit))
                    return false;
                usedSuits.Add(suit);
            }
            return true;
        }

        public bool Equal(Set other)
        {
            if(Count!= other.Count)
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

        public override string ToString()
        {
            string output = "";
            foreach(Card card in Cards)
                output += card + " ";
            return output.TrimEnd();
        }
    }
}