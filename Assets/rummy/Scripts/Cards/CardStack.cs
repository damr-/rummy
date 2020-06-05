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

        private CardStackType type;

        public enum CardStackType
        {
            DEFAULT = 0,
            NO_JOKER = 1,
            ONLY_JACKS = 2,
            ONLY_HEARTS = 3,
            CUSTOM = 4
        }

        public void CreateCardStack(CardStackType cardServeType)
        {
            if (cardStackCreated)
                throw new RummyException("Tried to create a stack but there already is one!");
            type = cardServeType;
            ReCreateCardStack();
            cardStackCreated = true;
        }

        private void ReCreateCardStack()
        {
            switch (type)
            {
                case CardStackType.DEFAULT:
                    CreateCardStack();
                    break;
                case CardStackType.NO_JOKER:
                    CreateCardStackNoJoker();
                    break;
                case CardStackType.ONLY_JACKS:
                    CreateJackCardStack();
                    break;
                case CardStackType.ONLY_HEARTS:
                    CreateHeartCardStack();
                    break;
                default: // TEST_CardServeType.CUSTOM
                    while (Cards.Count > 0)
                        Destroy(Cards.Pop().gameObject);
                    CreateCustomStack();
                    break;
            }
            if (type != CardStackType.CUSTOM)
                Cards = new Stack<Card>(Cards.OrderBy(x => Random.Range(0, int.MaxValue)));

            foreach (Card card in Cards)
                card.SetVisible(false);
        }

        /// <summary>
        /// Create two regular decks of cards with one red and one black joker each
        /// or 'order' the existing cards when a new game starts
        /// </summary>
        private void CreateCardStack()
        {
            for (int i = 0; i < 2; i++)
            {
                for (int suit = 1; suit <= Card.CardSuitCount; suit++)
                {
                    for (int rank = 2; rank <= Card.CardRankCount; rank++)
                    {
                        if (!cardStackCreated)
                            CreateCard((Card.CardRank)rank, (Card.CardSuit)suit);
                        else
                        {
                            int index = i * Card.CardSuitCount * (Card.CardRankCount - 1) + (suit - 1) * (Card.CardRankCount - 1) + (rank - 2);
                            Cards.ElementAt(Cards.Count - 1 - index).SetType((Card.CardRank)rank, (Card.CardSuit)suit);
                        }
                    }
                }
            }

            var j = Card.CardRank.JOKER;
            var c = Card.CardSuit.CLUBS;
            var d = Card.CardSuit.DIAMONDS;

            // One red and one black joker per deck
            if (!cardStackCreated)
            {
                CreateCard(j, c);
                CreateCard(j, c);
                CreateCard(j, d);
                CreateCard(j, d);
            }
            else
            {
                Cards.ElementAt(3).SetType(j, c);
                Cards.ElementAt(2).SetType(j, c);
                Cards.ElementAt(1).SetType(j, d);
                Cards.ElementAt(0).SetType(j, d);
            }
        }

        private void CreateCardStackNoJoker()
        {
            for (int i = 0; i < 2; i++)
            {
                for (int suit = 1; suit <= Card.CardSuitCount; suit++)
                {
                    for (int rank = 2; rank <= Card.CardRankCount; rank++)
                    {
                        if (!cardStackCreated)
                            CreateCard((Card.CardRank)rank, (Card.CardSuit)suit);
                        else
                        {
                            int index = i * Card.CardSuitCount * (Card.CardRankCount - 1) + (suit - 1) * (Card.CardRankCount - 1) + (rank - 2);
                            Cards.ElementAt(Cards.Count - 1 - index).SetType((Card.CardRank)rank, (Card.CardSuit)suit);
                        }
                    }
                }
            }
        }

        private void CreateJackCardStack()
        {
            for (int i = 0; i < 2 * Card.CardRankCount; i++)
            {
                for (int suit = 1; suit <= Card.CardSuitCount; suit++)
                {
                    if (!cardStackCreated)
                        CreateCard(Card.CardRank.JACK, (Card.CardSuit)suit);
                    else
                    {
                        int index = (suit - 1) + i * Card.CardSuitCount;
                        Cards.ElementAt(Cards.Count - 1 - index).SetType(Card.CardRank.JACK, (Card.CardSuit)suit);
                    }
                }
            }
        }

        private void CreateHeartCardStack()
        {
            for (int i = 0; i < 2 * Card.CardSuitCount; i++)
            {
                for (int rank = 1; rank <= Card.CardRankCount; rank++)
                {
                    if (!cardStackCreated)
                        CreateCard((Card.CardRank)rank, Card.CardSuit.HEARTS);
                    else
                    {
                        int index = (rank - 1) + i * Card.CardRankCount;
                        Cards.ElementAt(Cards.Count - 1 - index).SetType((Card.CardRank)rank, Card.CardSuit.HEARTS);
                    }
                }
            }
        }

        private void CreateCustomStack()
        {
            for (int i = 0; i < 10; i++)
            {
                CreateCard(Card.CardRank.THREE, Card.CardSuit.HEARTS);
                CreateCard(Card.CardRank.THREE, Card.CardSuit.HEARTS);
            }

            CreateCard(Card.CardRank.TEN, Card.CardSuit.HEARTS);
            CreateCard(Card.CardRank.ACE, Card.CardSuit.SPADES);

            CreateCard(Card.CardRank.THREE, Card.CardSuit.HEARTS);
            CreateCard(Card.CardRank.THREE, Card.CardSuit.HEARTS);

            CreateCard(Card.CardRank.TEN, Card.CardSuit.HEARTS);
            CreateCard(Card.CardRank.SIX, Card.CardSuit.CLUBS);

            CreateCard(Card.CardRank.TEN, Card.CardSuit.HEARTS);
            CreateCard(Card.CardRank.SEVEN, Card.CardSuit.CLUBS);

            CreateCard(Card.CardRank.TEN, Card.CardSuit.HEARTS);
            CreateCard(Card.CardRank.EIGHT, Card.CardSuit.CLUBS);

            CreateCard(Card.CardRank.TEN, Card.CardSuit.HEARTS);
            CreateCard(Card.CardRank.QUEEN, Card.CardSuit.SPADES);

            CreateCard(Card.CardRank.TEN, Card.CardSuit.HEARTS);
            CreateCard(Card.CardRank.QUEEN, Card.CardSuit.SPADES);

            CreateCard(Card.CardRank.TEN, Card.CardSuit.HEARTS);
            CreateCard(Card.CardRank.JACK, Card.CardSuit.SPADES);

            CreateCard(Card.CardRank.TEN, Card.CardSuit.HEARTS);
            CreateCard(Card.CardRank.JACK, Card.CardSuit.SPADES);
        }

        /// <summary>
        /// Creates a card GameObject with <see cref="Card.CardRank"/> 'rank'
        /// and <see cref="Card.CardSuit"/> 'suit' and adds it to the card stack.
        /// </summary>
        private void CreateCard(Card.CardRank rank, Card.CardSuit suit)
        {
            GameObject CardGO = Instantiate(CardPrefab, transform.position, Quaternion.identity, transform);
            Card card = CardGO.GetComponent<Card>();
            card.SetType(rank, suit);
            Cards.Push(card);
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
                card.transform.position = transform.position;
                card.transform.SetParent(transform, true);
                Cards.Push(card);
            }
            ReCreateCardStack();
        }
    }

}