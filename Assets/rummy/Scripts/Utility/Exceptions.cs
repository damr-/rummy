using System;

namespace rummy.Utility
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

}