namespace rummy.Cards
{

    public class Single
    {
        public Card Card { get; private set; }
        public CardSpot CardSpot { get; private set; }
        public override string ToString() => Card.ToString();

        ///<summary>The joker which will be replaced by this single card</summary>
        public Card Joker { get; private set; }
        ///<summary>(optional) The spot (index) in the Run where the card will be placed</summary>
        public int Spot { get; private set; }

        public Single(Card card, CardSpot cardSpot, Card joker, int spot = -1)
        {
            Card = card;
            CardSpot = cardSpot;
            Joker = joker;
            Spot = spot;
        }
    }

}