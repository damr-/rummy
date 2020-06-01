using System.Collections.Generic;
using UnityEngine;

namespace rummy.Cards
{

    public class DiscardStack : MonoBehaviour
    {
        private readonly Stack<Card> Cards = new Stack<Card>();
        public int CardCount => Cards.Count;

        public void AddCard(Card card)
        {
            card.transform.SetParent(transform, true);
            Cards.Push(card);
        }
        public Card DrawCard()
        {
            var card = Cards.Pop();
            card.transform.SetParent(null, true);
            return card;
        }
        public Card PeekCard() => Cards.Peek();
        public Vector3 GetNextCardPos() => transform.position + Vector3.up * 0.0001f * Cards.Count;

        /// <summary>
        /// Removes and returns all cards but the latest
        /// </summary>
        /// <returns>A list of all cards, except the one which was discarded last</returns>
        public List<Card> RecycleDiscardedCards()
        {
            Card lastDiscarded = Cards.Pop();
            var cards = RemoveCards();
            AddCard(lastDiscarded);
            lastDiscarded.transform.position = GetNextCardPos();
            return cards;
        }

        public List<Card> RemoveCards()
        {
            List<Card> removedCards = new List<Card>();
            while (Cards.Count > 0)
                removedCards.Add(DrawCard());
            return removedCards;
        }
    }

}