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

            foreach (Card card in Cards)
                card.SetVisible(false);

            stackCreated = true;
        }

        public void CreateCardStackNoJoker()
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
                        //No Joker cards
                        if ((Card.CardRank)rank == Card.CardRank.JOKER)
                            continue;

                        GameObject CardGO = Instantiate(CardPrefab, transform.position, Quaternion.identity);
                        Card card = CardGO.GetComponent<Card>();
                        card.Rank = (Card.CardRank)rank;
                        card.Suit = (Card.CardSuit)suit;
                        Cards.Push(card);
                    }
                }
            }

            foreach (Card card in Cards)
                card.SetVisible(false);

            stackCreated = true;
        }

        public void TEST_CreateJackCardStack()
        {
            if(stackCreated)
            {
                Debug.LogWarning("Tried to create a stack but there already is one!");
                return;
            }

            for (int i = 0; i < 2*Card.CardRankCount; i++)
            {
                for (int suit = 1; suit <= Card.CardSuitCount; suit++)
                {
                    GameObject CardGO = Instantiate(CardPrefab, transform.position, Quaternion.identity);
                    Card card = CardGO.GetComponent<Card>();
                    card.Rank = Card.CardRank.JACK;
                    card.Suit = (Card.CardSuit)suit;
                    Cards.Push(card);
                }
            }

            foreach (Card card in Cards)
                card.SetVisible(false);

            stackCreated = true;
        }

        public void TEST_CreateHeartCardStack()
        {
            if(stackCreated)
            {
                Debug.LogWarning("Tried to create a stack but there already is one!");
                return;
            }

            for (int i = 0; i < 2*Card.CardSuitCount; i++)
            {
                for (int rank = 1; rank <= Card.CardRankCount; rank++)
                {
                    //Don't create jokers
                    if ((Card.CardRank)rank == Card.CardRank.JOKER)
                        continue;

                    GameObject CardGO = Instantiate(CardPrefab, transform.position, Quaternion.identity);
                    Card card = CardGO.GetComponent<Card>();
                    card.Rank = (Card.CardRank)rank;
                    card.Suit = Card.CardSuit.HEART;
                    Cards.Push(card);
                }
            }

            foreach (Card card in Cards)
                card.SetVisible(false);

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

        /// <summary>
        /// Shuffles and adds the cards to the card stack. 
        /// Usually the card stack is restocked with the cards from the discard stack
        /// </summary>
        public void Restock(List<Card> cards)
        {
            foreach(var card in cards)
            {
                card.SetVisible(false);
                card.transform.position = transform.position;
                Cards.Push(card);
            }
            ShuffleCardStack();
        }
    }

}