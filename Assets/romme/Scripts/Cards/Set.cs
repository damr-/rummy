using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using romme.Utility;

namespace romme.Cards
{
    public class Set
    {
        public Card.CardRank Rank { get; private set; }
        public List<Card> Cards { get; private set; }
        public int Count => Cards.Count;
        //If the first card is a joker take the value of the second card times the number of cards
        public int Value => Cards.Count * (Cards[0].Rank != Card.CardRank.JOKER ? Cards[0].Value() : Cards[1].Value());

        public Set(Card c1, Card c2, Card c3) : this(new List<Card>() { c1, c2, c3 }) { }
        public Set(List<Card> cards)
        {
            if (!CardUtil.IsValidSet(cards))
            {
                Debug.LogWarning("The cards in a set are not all of the same rank or some share the same suit!");
                return;
            }

            Cards = new List<Card>() { };
            Cards.AddRange(cards);
            Rank = (Cards[0].Rank != Card.CardRank.JOKER) ? Cards[0].Rank : Cards[1].Rank;
        }

        public bool Equal(Set other)
        {
            if (Count != other.Count)
                return false;

            for (int i = 0; i < Count; i++)
            {
                for (int j = 0; j < other.Count; j++)
                {
                    if (Cards[i] != other.Cards[j])
                        return false;
                }
            }

            return true;
        }

        public override string ToString()
        {
            string output = "";
            foreach (Card card in Cards)
                output += card + " ";
            return output.TrimEnd();
        }
    }
}