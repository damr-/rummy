using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using romme.Utility;

namespace romme.Cards
{

    public class Single
    {
        public Card Card { get; private set; }
        public CardSpot CardSpot { get; private set; }

        ///<summary>
        /// The joker which this card will replace
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