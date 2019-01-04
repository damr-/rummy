using System.Collections.Generic;
using UnityEngine;

namespace romme.Cards
{

    public class DiscardStack : MonoBehaviour
    {
        public int CardCount { get { return Cards.Count; } }
        private Stack<Card> Cards = new Stack<Card>();

        public void AddCard(Card card)
        {
            Cards.Push(card);
        }

        public Card DrawCard()
        {
            return Cards.Pop();
        }

        public Vector3 GetNextCardPos()
        {
            return transform.position + Vector3.up * 0.001f * Cards.Count;
        }
    }

}