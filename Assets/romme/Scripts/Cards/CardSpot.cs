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
            if (Type == SpotType.RUN)
            {
                if (CardUtil.IsValidRun(Cards))
                    return new Run(Cards).Value;
                else
                    return 0;
            }
            else if (Type == SpotType.SET)
            {
                if (CardUtil.IsValidSet(Cards))
                    return new Set(Cards).Value;
                // else if (Cards.Count == 5) //When swapping for a joker, ther'll be 5 cards at the spot for some time and the value has to be calculated manually
                //     return (Cards.Count - 1) * Cards.GetFirstCard().Value;
                else //When the spot is currently under construction or a joker is being swapped out (so there's 5 cards and the set is invalid)
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

                //Find out the rank of the last card in the run
                int highestNonJokerIdx = CardUtil.GetFirstLowerNonJokerCardIdx(cards, cards.Count - 1);
                Card.CardRank highestRank = Card.CardRank.JOKER;
                if (highestNonJokerIdx != -1)
                {
                    highestRank = cards[highestNonJokerIdx].Rank + (cards.Count - 1 - highestNonJokerIdx);
                    if (highestNonJokerIdx == 0 && cards[highestNonJokerIdx].Rank == Card.CardRank.ACE)
                        highestRank = (Card.CardRank)(cards.Count);
                }
                
                //1st priority: add ace after king. If the highest rank is not a king,
                //add the ace at the beginning, in front of the TWO
                if (card.Rank == Card.CardRank.ACE && highestRank != Card.CardRank.KING)
                {
                    idx = 0;
                }
                else if (card.IsJoker()) //Joker will be added at the end, if possible
                {
                    if(highestRank == Card.CardRank.ACE)
                        idx = 0;
                    else
                        idx = cards.Count;
                }
                else //Any other case, the card will be sorted by rank
                {
                    for (int i = 0; i < cards.Count; i++)
                    {
                        var rank = cards[i].Rank;

                        if (cards[i].IsJoker()) //Figure out the rank of the card which the joker is replacing
                        {
                            if (i == 0)
                            {
                                //Joker is the only card in the run (when it is currently being laid down, for example)
                                //Therefore, the next card comes AFTER the joker
                                if (cards.Count == 1)
                                {
                                    idx = 1;
                                    break;
                                }
                                else
                                {
                                    var nonJokerIdx = CardUtil.GetFirstHigherNonJokerCardIdx(cards, 1);
                                    rank = cards[nonJokerIdx].Rank - nonJokerIdx;
                                }
                            }
                            else
                            {
                                //Try to find the first card below the current joker which isn't one
                                var nonJokerIdx = CardUtil.GetFirstLowerNonJokerCardIdx(cards, i - 1);
                                if (nonJokerIdx != -1)
                                {
                                    rank = cards[nonJokerIdx].Rank + (i - nonJokerIdx);
                                    if (nonJokerIdx == 0 && cards[nonJokerIdx].Rank == Card.CardRank.ACE)
                                        rank = (Card.CardRank)(i + 1);
                                }
                                else //No card below was found, search for higher card
                                {
                                    nonJokerIdx = CardUtil.GetFirstHigherNonJokerCardIdx(cards, i + 1);
                                    if (nonJokerIdx != -1)
                                        rank = cards[nonJokerIdx].Rank - (nonJokerIdx - i);
                                    else
                                        Debug.LogError("Rank of joker card could not be figured out! This should never happen!");
                                }
                            }
                        }
                        else if(rank == Card.CardRank.ACE && i == 0)
                            rank = (Card.CardRank)1;

                        if (rank > card.Rank)
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

                        Card joker = Cards.FirstOrDefault(c => c.IsJoker());
                        if (joker != null)
                        {
                            //Allow adding a third card of one color if one of the other two is a joker
                            if (set.HasTwoBlackCards() && joker.IsRed() && newCard.IsBlack())
                                return false;
                            if (set.HasTwoRedCards() && joker.IsBlack() && newCard.IsRed())
                                return false;

                            if (joker.Color == newCard.Color)
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

                        if (newCard.IsBlack() && set.HasTwoBlackCards())
                            return false;
                        if (newCard.IsRed() && set.HasTwoRedCards())
                            return false;
                        return true;
                    }
                default: //SpotType.RUN:
                    Run run = new Run(Cards);
                    var highestRank = run.GetHighestRank();
                    var lowestRank = run.GetLowestRank();
                    if (!newCard.IsJoker())
                    {
                        if (newCard.Suit != run.Suit)
                            return false;

                        var jokers = Cards.Where(c => c.IsJoker());
                        Card replacedJoker = null;

                        foreach (var joker in jokers)
                        {
                            Card.CardRank actualJokerRank = Card.CardRank.JOKER;
                            int jokerIdx = Cards.IndexOf(joker);
                            int higherNonJokerIdx = CardUtil.GetFirstHigherNonJokerCardIdx(Cards, jokerIdx + 1);
                            if (higherNonJokerIdx != -1)
                                actualJokerRank = Cards[higherNonJokerIdx].Rank - (higherNonJokerIdx - jokerIdx);
                            else
                            {
                                int lowerNonJokerIdx = CardUtil.GetFirstLowerNonJokerCardIdx(Cards, jokerIdx - 1);
                                if (lowerNonJokerIdx != -1)
                                    actualJokerRank = Cards[lowerNonJokerIdx].Rank + (jokerIdx - lowerNonJokerIdx);
                                else
                                    Debug.LogError("Rank of joker card could not be figured out! This should never happen!");
                            }

                            if (actualJokerRank == newCard.Rank)
                            {
                                replacedJoker = joker;
                                break;
                            }
                        }

                        if (replacedJoker != null)
                        {
                            Joker = replacedJoker;
                            return true;
                        }

                        return (newCard.Rank == highestRank + 1 && highestRank != Card.CardRank.ACE) ||
                                (newCard.Rank == lowestRank - 1 && lowestRank != Card.CardRank.ACE) ||
                                (newCard.Rank == Card.CardRank.ACE && lowestRank == Card.CardRank.TWO);
                    }
                    else
                    {
                        return newCard.Color == run.GetRunColor() && (highestRank != Card.CardRank.ACE || lowestRank != Card.CardRank.ACE);
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