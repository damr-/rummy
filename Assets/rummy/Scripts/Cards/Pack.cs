using System;
using System.Collections.Generic;
using rummy.Utility;

namespace rummy.Cards
{
    public class RummyException : Exception
    {
        public RummyException(string message) : base(AttachPrefix(message)) { }
        private static string AttachPrefix(string message)
        {
            string prefix = "[Seed " + Tb.I.GameMaster.Seed + ", Round " + Tb.I.GameMaster.RoundCount + "] ";
            return prefix + message;
        }
    }

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