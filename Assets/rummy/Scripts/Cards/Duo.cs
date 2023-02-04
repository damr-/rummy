using System.Collections.Generic;

namespace rummy.Cards
{

    public class Duo
    {
        public Card A { get; private set; }
        public Card B { get; private set; }
        public int Value { get; private set; }

        public Duo(Card card1, Card card2)
        {
            A = card1;
            B = card2;

            if (A.Rank == Card.CardRank.ACE)
                Value = 1 + B.Value;
            else
                Value = A.Value + B.Value;
        }

        public IEnumerable<Card> GetList() => new List<Card>() { A, B };
        public override string ToString() => A.ToString() + B.ToString();

        /// <summary>
        /// Return whether both duos contain one identical card
        /// and the other two cards look alike but are not the same
        /// </summary>
        public static bool AreHalfDuplicates(Duo d1, Duo d2)
        {
            if (d1.A == d2.A && d1.B == d2.B)
                return false;
            if (d1.A.LooksLike(d2.A) && d1.B == d2.B ||
                d1.B.LooksLike(d2.B) && d1.A == d2.A)
                return true;
            return false;
        }
    }

}