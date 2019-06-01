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

        public enum CardStackType
        {
            DEFAULT = 0,
            NO_JOKER = 1,
            ONLY_JACKS = 2,
            ONLY_HEARTS = 3,
            CUSTOM = 4
        }

        private void CreateCardStack()
        {
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
                        CreateCard((Card.CardRank)rank, (Card.CardSuit)suit);
                    }
                }
            }
        }

        private void CreateCardStackNoJoker()
        {
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
                        CreateCard((Card.CardRank)rank, (Card.CardSuit)suit);
                    }
                }
            }

            foreach (Card card in Cards)
                card.SetVisible(false);

            stackCreated = true;
        }

        private void TEST_CreateJackCardStack()
        {
            for (int i = 0; i < 2*Card.CardRankCount; i++)
            {
                for (int suit = 1; suit <= Card.CardSuitCount; suit++)
                {
                    CreateCard(Card.CardRank.JACK, (Card.CardSuit)suit);
                }
            }
        }

        private void TEST_CreateHeartCardStack()
        {
            for (int i = 0; i < 2*Card.CardSuitCount; i++)
            {
                for (int rank = 1; rank <= Card.CardRankCount; rank++)
                {
                    //Don't create jokers
                    if ((Card.CardRank)rank == Card.CardRank.JOKER)
                        continue;
                    CreateCard((Card.CardRank)rank, Card.CardSuit.HEART);
                }
            }
        }
        
        private void TEST_CreateCustomStack()
        {
            for(int i = 0; i < 100; i++)
                CreateCard(Card.CardRank.TEN, Card.CardSuit.HEART);

            // CreateCard(Card.CardRank.FIVE, Card.CardSuit.TILE);
            // CreateCard(Card.CardRank.JOKER, Card.CardSuit.TILE);

            // CreateCard(Card.CardRank.SIX, Card.CardSuit.HEART);
            // CreateCard(Card.CardRank.TWO, Card.CardSuit.PIKE);

            // CreateCard(Card.CardRank.KING, Card.CardSuit.PIKE);
            // CreateCard(Card.CardRank.TWO, Card.CardSuit.HEART);

            // CreateCard(Card.CardRank.FOUR, Card.CardSuit.CLOVERS);
            // CreateCard(Card.CardRank.JOKER, Card.CardSuit.CLOVERS);

            // CreateCard(Card.CardRank.THREE, Card.CardSuit.HEART);
            // CreateCard(Card.CardRank.TWO, Card.CardSuit.PIKE);

            // CreateCard(Card.CardRank.TWO, Card.CardSuit.PIKE);
            // CreateCard(Card.CardRank.TWO, Card.CardSuit.HEART);


            CreateCard(Card.CardRank.FIVE, Card.CardSuit.TILE);
            CreateCard(Card.CardRank.TWO, Card.CardSuit.HEART);

            CreateCard(Card.CardRank.SIX, Card.CardSuit.HEART);
            CreateCard(Card.CardRank.TWO, Card.CardSuit.CLOVERS);

            CreateCard(Card.CardRank.KING, Card.CardSuit.PIKE);
            CreateCard(Card.CardRank.TWO, Card.CardSuit.TILE);

            CreateCard(Card.CardRank.FOUR, Card.CardSuit.CLOVERS);
            CreateCard(Card.CardRank.TWO, Card.CardSuit.CLOVERS);

            CreateCard(Card.CardRank.THREE, Card.CardSuit.HEART);
            CreateCard(Card.CardRank.TWO, Card.CardSuit.PIKE);

            CreateCard(Card.CardRank.TWO, Card.CardSuit.PIKE);
            CreateCard(Card.CardRank.TWO, Card.CardSuit.HEART);
        }

        public void CreateCardStack(CardStackType cardServeType)
        {
            if(stackCreated)
            {
                Debug.LogWarning("Tried to create a stack but there already is one!");
                return;
            }

            switch (cardServeType)
            {
                case CardStackType.DEFAULT:
                    Tb.I.CardStack.CreateCardStack();
                    break;
                case CardStackType.NO_JOKER:
                    Tb.I.CardStack.CreateCardStackNoJoker();
                    break;
                case CardStackType.ONLY_JACKS:
                    Tb.I.CardStack.TEST_CreateJackCardStack();
                    break;
                case CardStackType.ONLY_HEARTS:
                    Tb.I.CardStack.TEST_CreateHeartCardStack();
                    break;
                default: // TEST_CardServeType.CUSTOM
                    Tb.I.CardStack.TEST_CreateCustomStack();
                    break;
            }

            foreach (Card card in Cards)
                card.SetVisible(false);
            stackCreated = true;
        }

        private void CreateCard(Card.CardRank rank, Card.CardSuit suit)
        {
            GameObject CardGO = Instantiate(CardPrefab, transform.position, Quaternion.identity);
            Card card = CardGO.GetComponent<Card>();
            card.Rank = rank;
            card.Suit = suit;
            Cards.Push(card);
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

        public void ResetStack()
        {
            stackCreated = false;
            while(Cards.Count > 0)
            {
                var card = Cards.Pop();
                Destroy(card.gameObject);
            }
        }
    }

}