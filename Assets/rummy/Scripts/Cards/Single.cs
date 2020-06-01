namespace rummy.Cards
{

    public class Single
    {
        public Card Card { get; private set; }
        public CardSpot CardSpot { get; private set; }
        public override string ToString() => Card.ToString();

        ///<summary>
        /// The joker which will be replaced by this single card
        ///</summary>
        public Card Joker { get; private set; }

        public Single(Card card, CardSpot cardSpot) : this(card, cardSpot, null) { }
        public Single(Card card, CardSpot cardSpot, Card joker)
        {
            Card = card;
            CardSpot = cardSpot;
            Joker = joker;
        }
    }

}