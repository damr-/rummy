using System.Collections.Generic;
using UnityEngine;
using romme.Utility;

namespace romme.Cards
{

    public class CardStack : MonoBehaviour
    {
        public GameObject CardPrefab;
        public int CardCount => Cards.Count;

        private Stack<Card> Cards = new Stack<Card>();
        private bool stackCreated;

        public void CreateCardStack()
        {
            if(stackCreated)
            {
                Debug.LogWarning("Tried to create a stack but there already is one!");
                return;
            }

            //Spawn two decks of cards
            for (int i = 0; i < 2; i++)
            {
                for (int suit = 1; suit <= Card.CardSuitCount; suit++)
                {
                    for (int rank = 1; rank <= Card.CardRankCount; rank++)
                    {
                        //If card is joker, only spawn if suit is 1 or 3, 
                        //so we only have one red and one black joker
                        if ((Card.CardRank)rank == Card.CardRank.JOKER && suit % 2 != 1)
                            continue;

                        GameObject CardGO = Instantiate(CardPrefab, transform.position, Quaternion.identity);
                        Card card = CardGO.GetComponent<Card>();
                        card.Rank = (Card.CardRank)rank;
                        card.Suit = (Card.CardSuit)suit;
                        Cards.Push(card);
                    }
                }
            }

            foreach (Card c in Cards)
                c.SetVisible(false);

            stackCreated = true;
        }

        public void ShuffleCardStack()
        {
            Cards = Cards.Shuffle();
        }

        public Card DrawCard()
        {
            return Cards.Pop();
        }
    }

}