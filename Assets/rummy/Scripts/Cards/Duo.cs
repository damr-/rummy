using System.Collections.Generic;

namespace rummy.Cards
{

    public class Duo
    {
        public Card A { get; private set; }
        public Card B { get; private set; }
        public int Value() => A.Value + B.Value;
        public IEnumerable<Card> GetList() => new List<Card>() { A, B };

        public Duo(Card card1, Card card2)
        {
            A = card1;
            B = card2;
        }

        public override string ToString()
        {
            return A.ToString() + B.ToString();
        }

    }

}