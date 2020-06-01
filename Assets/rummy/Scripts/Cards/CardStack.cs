using System.Collections.Generic;
using UnityEngine;
using rummy.Utility;
using System.Linq;

namespace rummy.Cards
{

    public class CardStack : MonoBehaviour
    {
        public GameObject CardPrefab;

        private Stack<Card> Cards = new Stack<Card>();
        public int CardCount => Cards.Count;
        public bool cardStackCreated = false;

        public enum CardStackType
        {
            DEFAULT = 0,
            NO_JOKER = 1,
            ONLY_JACKS = 2,
            ONLY_HEARTS = 3,
            CUSTOM = 4
        }

        /// <summary>
        /// Spawn two regular decks of cards with one red and one black joker each
        /// </summary>
        private void CreateCardStack()
        {
            for (int i = 0; i < 2; i++)
            {
                for (int suit = 1; suit <= Card.CardSuitCount; suit++)
                {
                    for (int rank = 1; rank <= Card.CardRankCount; rank++)
                    {
                        //Spawn one red and one black joker per deck
                        if ((Card.CardRank)rank == Card.CardRank.JOKER && suit % 2 != 1)
                            continue;
                        CreateCard((Card.CardRank)rank, (Card.CardSuit)suit);
                    }
                }
            }
        }

        /// <summary>
        /// Spawn two decks of cards without Jokers
        /// </summary>
        private void CreateCardStackNoJoker()
        {
            for (int i = 0; i < 2; i++)
            {
                for (int suit = 1; suit <= Card.CardSuitCount; suit++)
                {
                    for (int rank = 1; rank <= Card.CardRankCount; rank++)
                    {
                        if ((Card.CardRank)rank == Card.CardRank.JOKER)
                            continue;
                        CreateCard((Card.CardRank)rank, (Card.CardSuit)suit);
                    }
                }
            }

            foreach (Card card in Cards)
                card.SetVisible(false);
        }

        private void TEST_CreateJackCardStack()
        {
            for (int i = 0; i < 2 * Card.CardRankCount; i++)
            {
                for (int suit = 1; suit <= Card.CardSuitCount; suit++)
                {
                    CreateCard(Card.CardRank.JACK, (Card.CardSuit)suit);
                }
            }
        }

        private void TEST_CreateHeartCardStack()
        {
            for (int i = 0; i < 2 * Card.CardSuitCount; i++)
            {
                for (int rank = 1; rank <= Card.CardRankCount; rank++)
                {
                    //Don't create jokers
                    if ((Card.CardRank)rank == Card.CardRank.JOKER)
                        continue;
                    CreateCard((Card.CardRank)rank, Card.CardSuit.HEARTS);
                }
            }
        }

        private void TEST_CreateCustomStack()
        {
            for (int i = 0; i < 4; i++)
            {
                CreateCard(Card.CardRank.TEN, Card.CardSuit.HEARTS);
                CreateCard(Card.CardRank.TEN, Card.CardSuit.HEARTS);
            }

            CreateCard(Card.CardRank.TEN, Card.CardSuit.HEARTS);
            CreateCard(Card.CardRank.SIX, Card.CardSuit.HEARTS);

            CreateCard(Card.CardRank.TEN, Card.CardSuit.HEARTS);
            CreateCard(Card.CardRank.ACE, Card.CardSuit.SPADES);

            CreateCard(Card.CardRank.TEN, Card.CardSuit.HEARTS);
            CreateCard(Card.CardRank.JOKER, Card.CardSuit.SPADES);

            CreateCard(Card.CardRank.TEN, Card.CardSuit.HEARTS);
            CreateCard(Card.CardRank.TEN, Card.CardSuit.HEARTS);
            CreateCard(Card.CardRank.TEN, Card.CardSuit.HEARTS);
            CreateCard(Card.CardRank.TEN, Card.CardSuit.HEARTS);
            CreateCard(Card.CardRank.TEN, Card.CardSuit.HEARTS);
            CreateCard(Card.CardRank.TEN, Card.CardSuit.HEARTS);

            CreateCard(Card.CardRank.TEN, Card.CardSuit.HEARTS);
            CreateCard(Card.CardRank.JACK, Card.CardSuit.SPADES);

            CreateCard(Card.CardRank.TEN, Card.CardSuit.HEARTS);
            CreateCard(Card.CardRank.KING, Card.CardSuit.SPADES);

            CreateCard(Card.CardRank.TEN, Card.CardSuit.HEARTS);
            CreateCard(Card.CardRank.QUEEN, Card.CardSuit.SPADES);
        }

        public void CreateCardStack(CardStackType cardServeType)
        {
            if (cardStackCreated)
                throw new RummyException("Tried to create a stack but there already is one!");

            switch (cardServeType)
            {
                case CardStackType.DEFAULT:
                    CreateCardStack();
                    break;
                case CardStackType.NO_JOKER:
                    CreateCardStackNoJoker();
                    break;
                case CardStackType.ONLY_JACKS:
                    TEST_CreateJackCardStack();
                    break;
                case CardStackType.ONLY_HEARTS:
                    TEST_CreateHeartCardStack();
                    break;
                default: // TEST_CardServeType.CUSTOM
                    TEST_CreateCustomStack();
                    break;
            }

            if (cardServeType != CardStackType.CUSTOM)
                ShuffleCardStack();

            foreach (Card card in Cards)
                card.SetVisible(false);
            cardStackCreated = true;
        }

        /// <summary>
        /// Creates a card GameObject with <see cref="Card.CardRank"/> <see cref="rank"/>
        /// and <see cref="Card.CardSuit"/> <see cref="suit"/> and adds it to the card stack.
        /// </summary>
        /// <param name="rank">The <see cref="Card.CardRank"/> of the new card</param>
        /// <param name="suit">The <see cref="Card.CardSuit"/> of the new card</param>
        private void CreateCard(Card.CardRank rank, Card.CardSuit suit)
        {
            GameObject CardGO = Instantiate(CardPrefab, transform.position, Quaternion.identity, transform);
            Card card = CardGO.GetComponent<Card>();
            card.SetType(rank, suit);
            Cards.Push(card);
        }

        private void ShuffleCardStack()
        {
            Cards = new Stack<Card>(Cards.OrderBy(x => Random.Range(0, int.MaxValue)));
        }

        /// <summary>
        /// Removes the next card from the cardstack and returns it, if possible.
        /// </summary>
        /// <returns>The next card which was removed from the stack, null otherwise</returns>
        public Card DrawCard()
        {
            if (CardCount == 0)
                throw new RummyException("CardStack is empty!");
            var card = Cards.Pop();
            card.transform.SetParent(null, true);
            return card;
        }

        /// <summary>
        /// Adds the given list of cards to the card stack and shuffles it.
        /// Usually the card stack is restocked with the cards from the discard stack
        /// or all cards from the game when it is over
        /// </summary>
        public void Restock(List<Card> cards)
        {
            foreach (var card in cards)
            {
                card.SetVisible(false);
                card.transform.position = transform.position;
                card.transform.SetParent(transform, true);
                Cards.Push(card);
            }
            ShuffleCardStack();
        }
    }

}