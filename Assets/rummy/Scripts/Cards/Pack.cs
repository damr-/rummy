using System;
using System.Collections.Generic;
using rummy.Utility;

namespace rummy.Cards
{

    public abstract class Pack
    {
        public List<Card> Cards { get; protected set; }
        public int Count => Cards.Count;
        public int Value { get; protected set; } = 0;

        public override string ToString()
        {
            string output = "";
            foreach (var card in Cards)
                output += card + " ";
            return output.TrimEnd();
        }

        /// <summary>
        /// Returns whether this card pack intersects the other (whether the two have at least one card in common)
        /// </summary>
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