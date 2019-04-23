using System.Collections.Generic;
using UnityEngine;

namespace romme.Cards
{

    public class DiscardStack : MonoBehaviour
    {
        public int CardCount { get { return Cards.Count; } }
        private Stack<Card> Cards = new Stack<Card>();

        public void AddCard(Card card) => Cards.Push(card);
        public Card DrawCard() => Cards.Pop();
        public Card PeekCard() => Cards.Peek();
        public Vector3 GetNextCardPos() => transform.position + Vector3.up * 0.001f * Cards.Count;

        public List<Card> RemoveCards()
        {
            List<Card> cards = new List<Card>();
            while(Cards.Count > 0)
                cards.Add(Cards.Pop());
            return cards;
        }
    }

}