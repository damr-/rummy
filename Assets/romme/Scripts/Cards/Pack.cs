using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using romme.Utility;

namespace romme.Cards
{

    public abstract class Pack
    {
        public List<Card> Cards { get; protected set; }
        public int Count => Cards.Count;

        public Card RemoveLastCard()
        {
            if (Cards.Count == 0)
                return null;
            Card card = Cards[Cards.Count - 1];
            Cards.Remove(card);
            return card;
        }

        public override string ToString()
        {
            string output = "";
            foreach (var card in Cards)
                output += card + " ";
            return output.TrimEnd();
        }

        public bool Intersects(Pack other) => Cards.Intersects(other.Cards);

        public bool Equal(Pack other)
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
    }

}