using System.Collections.Generic;
using System.Linq;
using romme.Utility;
using UnityEngine;

namespace romme.Cards
{
    public class CardSpot : MonoBehaviour
    {
        public float startAngle;
        public float cardRadius = 2f;
        public float cardsAngleSpread = 180f;

        public enum SpotType
        {
            NONE,
            RUN,
            SET
        }

        public SpotType Type;

        public int GetValue()
        {
            if (Type == SpotType.RUN && CardUtil.IsValidRun(Cards))
                return new Run(Cards).Value;
            else if (Type == SpotType.SET)
            {
                if(CardUtil.IsValidSet(Cards))
                    return new Set(Cards).Value;
                else if(Cards.Count == 5) //When swapping for a joker, ther'll be 5 cards at the spot for some time and the value has to be calculated manually
                    return (Cards.Count - 1) * Cards.GetFirstCard().Value;
                else //When the spot is currently
                    return 0;
            }
            else
            {
                int value = 0;
                foreach (var card in Cards)
                {
                    if (card.IsJoker())
                        value += 20;
                    else
                        value += card.Value;
                }
                return value;
            }
        }

        public List<Card> Cards { get; private set; } = new List<Card>();
        public bool HasCards => Cards.Count > 0;

        public void AddCard(Card card)
        {
            var cards = new List<Card>(Cards);

            if (Type == SpotType.RUN && cards.Count > 0)
            {
                int idx = cards.Count; //By default, add the new card at the end
                var highestRank = Cards[cards.Count - 1].Rank;
                //1st priority: add ace after king. If the highest rank card in the run is not a king,
                //manually add the ace at the beginning, in front of the TWO
                if (card.Rank == Card.CardRank.ACE && highestRank != Card.CardRank.KING)
                {
                    idx = 0;
                }
                //If the first item in the run is an ACE, the new card can only be added at the end
                else if (cards[0].Rank == Card.CardRank.ACE)
                {
                    idx = cards.Count;
                }
                else //Any other case, the card will be sorted by rank
                {
                    for (int i = 0; i < cards.Count; i++)
                    {
                        if (cards[i].Rank > card.Rank)
                        {
                            idx = i;
                            break;
                        }
                    }
                }
                cards.Insert(idx, card);
            }
            else
                cards.Add(card);
            Cards = new List<Card>(cards);
        }

        public void RemoveCard(Card card) => Cards = Cards.Where(c => c != card).ToList();

        private void Update()
        {
            if (Cards.Count == 0)
                return;

            float deltaAngle = cardsAngleSpread / Cards.Count;

            for (int i = 0; i < Cards.Count; i++)
            {
                float x = cardRadius * Mathf.Cos((startAngle + i * deltaAngle) * Mathf.PI / 180f);
                float z = cardRadius * Mathf.Sin((startAngle + i * deltaAngle) * Mathf.PI / 180f);
                Cards[i].transform.position = transform.position + new Vector3(x, -0.1f * i, z);
            }
        }

        public bool CanFit(Card newCard, out Card Joker)
        {
            Joker = null;
            switch (Type)
            {
                case SpotType.NONE: return false;
                case SpotType.SET:
                    Set set = new Set(Cards);
                    if (!newCard.IsJoker())
                    {
                        if (newCard.Rank != set.Rank)
                            return false;

                        Card joker = Cards.Where(c => c.IsJoker()).FirstOrDefault();
                        if (joker != null)
                        {
                            //Allow adding a third card of one color if one of the other two is a joker
                            if (set.HasTwoBlacks() && joker.IsRed() && newCard.IsBlack())
                                return false;
                            if (set.HasTwoReds() && joker.IsBlack() && newCard.IsRed())
                                return false;

                            if(joker.Color == newCard.Color)
                                Joker = joker;

                            //Otherwise, only allow adding a card whose suit is not already there
                            var nonJokers = Cards.Where(c => !c.IsJoker());
                            return nonJokers.All(c => c.Suit != newCard.Suit);
                        }
                        else
                            return Cards.All(c => c.Suit != newCard.Suit);
                    }
                    else
                    {
                        //Don't allow more than one joker
                        if (Cards.Any(c => c.IsJoker()))
                            return false;

                        if (newCard.IsBlack() && set.HasTwoBlacks())
                            return false;
                        if (newCard.IsRed() && set.HasTwoReds())
                            return false;
                        return true;
                    }
                default: //SpotType.RUN:
                    Run run = new Run(Cards);
                    if (!newCard.IsJoker())
                    {
                        if (newCard.Suit != run.Suit)
                            return false;

                        var highestRank = run.GetHighestRank();
                        var lowestRank = run.GetLowestRank();

                        return (newCard.Rank == highestRank + 1 && highestRank != Card.CardRank.ACE) ||
                                (newCard.Rank == lowestRank - 1 && lowestRank != Card.CardRank.ACE) ||
                                (newCard.Rank == Card.CardRank.ACE && lowestRank == Card.CardRank.TWO);
                    }
                    else
                    {
                        return newCard.Color == run.GetRunColor();
                    }
            }
        }

        public void ResetSpot()
        {
            while (Cards.Count > 0)
            {
                Card c = Cards[0];
                Cards.RemoveAt(0);
                Destroy(c.gameObject);
            }
            Type = SpotType.NONE;
            Cards = new List<Card>();
        }
    }

}