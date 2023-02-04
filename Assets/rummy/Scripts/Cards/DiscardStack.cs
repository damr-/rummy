using System.Collections.Generic;
using UnityEngine;

namespace rummy.Cards
{

    public class DiscardStack : MonoBehaviour
    {
        private readonly Stack<Card> Cards = new();
        public int CardCount => Cards.Count;

        public void AddCard(Card card)
        {
            if(CardCount > 0)
                TopmostCard().SetVisible(false);
            Cards.Push(card);
            card.SetVisible(true);
        }

        public Card DrawCard()
        {
            var card = Cards.Pop();
            if (CardCount > 0)
                TopmostCard().SetVisible(true);
            return card;
        }

        public Card TopmostCard() => Cards.Peek();

        /// <summary>
        /// Remove and return all cards except the latest
        /// </summary>
        /// <returns>A list of all cards, except the one which was discarded last</returns>
        public List<Card> RecycleDiscardedCards()
        {
            Card lastDiscarded = Cards.Pop();

            List<Card> cards = new();
            while (CardCount > 0)
                cards.Add(DrawCard());

            AddCard(lastDiscarded);
            return cards;
        }

        public void RemoveCards()
        {
            while (CardCount > 0)
                Destroy(Cards.Pop().gameObject);
        }
    }

}