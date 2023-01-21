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
        /// Removes and returns all cards but the latest
        /// </summary>
        /// <returns>A list of all cards, except the one which was discarded last</returns>
        public List<Card> RecycleDiscardedCards()
        {
            Card lastDiscarded = Cards.Pop();
            var cards = RemoveCards();
            AddCard(lastDiscarded);
            return cards;
        }

        public List<Card> RemoveCards()
        {
            List<Card> removedCards = new();
            while (CardCount > 0)
                removedCards.Add(DrawCard());
            return removedCards;
        }
    }

}