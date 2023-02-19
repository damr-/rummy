using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using rummy.Utility;

namespace rummy.Cards
{

    public class CardStack : MonoBehaviour
    {
        public GameObject CardPrefab;
        /// <summary>
        /// Number of card decks in the stack
        /// </summary>
        public int CardDeckCount = 2;

        private Stack<Card> Cards = new();
        public int CardCount => Cards.Count;

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
            type = cardServeType;
            ReCreateCardStack();
        }

        /// <summary>
        /// Destroy all current cards, recreate the whole stack, and shuffle it
        /// </summary>
        private void ReCreateCardStack()
        {
            while (CardCount > 0)
                Destroy(Cards.Pop().gameObject);
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
                    CreateCustomStack();
                    break;
            }
            FinalizeCards();
        }

        /// <summary>
        /// Add the given list of cards to the card stack and shuffles it
        /// </summary>
        public void Restock(List<Card> cards)
        {
            foreach (var card in cards)
            {
                card.transform.position = transform.position;
                Cards.Push(card);
            }
            FinalizeCards();
        }

        /// <summary>
        /// Shuffle the cards (unless card stack type is custom) and set their visibilities and turned states
        /// </summary>
        private void FinalizeCards()
        {
            if (type != CardStackType.CUSTOM)
                Cards = new Stack<Card>(Cards.OrderBy(x => Random.Range(0, int.MaxValue)));
            foreach (var card in Cards)
            {
                card.SetVisible(false);
                card.SetTurned(true);
            }
            Cards.Peek().SetVisible(true);
        }

        /// <summary>
        /// Remove the next card from the cardstack and return it, if possible.
        /// </summary>
        /// <returns>The next card which was removed from the stack</returns>
        public Card DrawCard()
        {
            if (CardCount == 0)
                throw new RummyException("CardStack is empty");
            var card = Cards.Pop();
            if (CardCount > 0)
            {
                var next = Cards.Peek();
                next.SendToBackground(true);
                next.SetVisible(true);
            }
            card.SendToBackground(false);
            return card;
        }

        /// <summary>
        /// Create a card GameObject with <see cref="Card.CardRank"/> 'rank'
        /// and <see cref="Card.CardSuit"/> 'suit' and add it to the card stack.
        /// </summary>
        private void CreateCard(Card.CardRank rank, Card.CardSuit suit)
        {
            GameObject CardGO = Instantiate(CardPrefab, transform.position, Quaternion.identity, transform);
            Card card = CardGO.GetComponent<Card>();
            card.SetType(rank, suit);
            Cards.Push(card);
        }

        /// <summary>
        /// Create <see cref="CardDeckCount"/> regular decks of cards with one red and one black joker each
        /// </summary>
        private void CreateCardStack()
        {
            for (int i = 0; i < CardDeckCount; i++)
            {
                for (int suit = 1; suit <= Card.CardSuitCount; suit++)
                {
                    for (int rank = 2; rank <= Card.CardRankCount; rank++)
                    {
                        CreateCard((Card.CardRank)rank, (Card.CardSuit)suit);
                    }
                }
            }

            var j = Card.CardRank.JOKER;
            var c = Card.CardSuit.CLUBS;
            var d = Card.CardSuit.DIAMONDS;

            // One red and one black joker per deck
            CreateCard(j, c);
            CreateCard(j, c);
            CreateCard(j, d);
            CreateCard(j, d);
        }

        private void CreateCardStackNoJoker()
        {
            for (int i = 0; i < CardDeckCount; i++)
            {
                for (int suit = 1; suit <= Card.CardSuitCount; suit++)
                {
                    for (int rank = 2; rank <= Card.CardRankCount; rank++)
                    {
                        CreateCard((Card.CardRank)rank, (Card.CardSuit)suit);
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
                    CreateCard(Card.CardRank.JACK, (Card.CardSuit)suit);
                }
            }
        }

        private void CreateHeartCardStack()
        {
            for (int i = 0; i < 2 * Card.CardSuitCount; i++)
            {
                for (int rank = 1; rank <= Card.CardRankCount; rank++)
                {
                    CreateCard((Card.CardRank)rank, Card.CardSuit.HEARTS);
                }
            }
        }

        private void CreateCustomStack()
        {
            for (int i = 0; i < 10; i++)
            {
                CreateCard(Card.CardRank.TEN, Card.CardSuit.HEARTS);
                CreateCard(Card.CardRank.TEN, Card.CardSuit.HEARTS);
            }

            CreateCard(Card.CardRank.FIVE, Card.CardSuit.CLUBS);
            CreateCard(Card.CardRank.FIVE, Card.CardSuit.SPADES);
            CreateCard(Card.CardRank.FOUR, Card.CardSuit.CLUBS);
            CreateCard(Card.CardRank.FOUR, Card.CardSuit.SPADES);
            CreateCard(Card.CardRank.THREE, Card.CardSuit.CLUBS);
            CreateCard(Card.CardRank.THREE, Card.CardSuit.SPADES);
        }
    }

}