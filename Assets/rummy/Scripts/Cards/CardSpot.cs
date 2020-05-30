using System.Collections.Generic;
using System.Linq;
using rummy.Utility;
using UnityEngine;

namespace rummy.Cards
{
    public class CardSpot : RadialLayout<Card>
    {
        public override List<Card> Objects { get; protected set; } = new List<Card>();
        public bool HasCards => Objects.Count > 0;

        [Tooltip("The factor by which cards will be scaled when added to the spot. When removed, the scaling is undone")]
        public float CardScale = 1.0f;

        public enum SpotType
        {
            NONE,
            RUN,
            SET
        }
        public SpotType Type;

        protected override void InitValues()
        {
            yIncrement = -0.01f;
        }

        public override void ResetLayout()
        {
            Type = SpotType.NONE;
            base.ResetLayout();
        }

        public int GetValue()
        {
            int value = 0;
            if (Type == SpotType.RUN)
            {
                try { value = new Run(Objects).Value; }
                catch (RummyException) { }
            }
            else if (Type == SpotType.SET)
            {
                try { value = new Set(Objects).Value; }
                catch (RummyException) { }
            }
            else
            {
                foreach (var card in Objects)
                    value += card.IsJoker() ? 20 : card.Value;
            }
            return value;
        }

        public void AddCard(Card card)
        {
            var cards = new List<Card>(Objects);

            if (Type == SpotType.RUN && cards.Count > 0)
            {
                int idx = cards.Count; //By default, add the new card at the end

                //Find out the rank of the last card in the run
                int highestNonJokerIdx = CardUtil.GetFirstNonJokerCardIdx(cards, cards.Count - 1, false);
                Card.CardRank highestRank = Card.CardRank.JOKER;
                if (highestNonJokerIdx != -1)
                {
                    highestRank = cards[highestNonJokerIdx].Rank + (cards.Count - 1 - highestNonJokerIdx);
                    if (highestNonJokerIdx == 0 && cards[highestNonJokerIdx].Rank == Card.CardRank.ACE)
                        highestRank = (Card.CardRank)cards.Count;
                }

                //1st priority: add ace after king. If the highest rank is not a king,
                //add the ace at the beginning, in front of the TWO
                if (card.Rank == Card.CardRank.ACE && highestRank != Card.CardRank.KING)
                {
                    idx = 0;
                }
                else if (card.IsJoker()) //Joker will be added at the end, if possible
                {
                    if (highestRank == Card.CardRank.ACE)
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
                                    var nonJokerIdx = CardUtil.GetFirstNonJokerCardIdx(cards, 1, true);
                                    rank = cards[nonJokerIdx].Rank - nonJokerIdx;
                                }
                            }
                            else
                            {
                                //Try to find the first card below the current joker which isn't one
                                var nonJokerIdx = CardUtil.GetFirstNonJokerCardIdx(cards, i - 1, false);
                                if (nonJokerIdx != -1)
                                {
                                    rank = cards[nonJokerIdx].Rank + (i - nonJokerIdx);
                                    if (nonJokerIdx == 0 && cards[nonJokerIdx].Rank == Card.CardRank.ACE)
                                        rank = (Card.CardRank)(i + 1);
                                }
                                else //No card below was found, search for higher card
                                {
                                    nonJokerIdx = CardUtil.GetFirstNonJokerCardIdx(cards, i + 1, true);
                                    if (nonJokerIdx != -1)
                                        rank = cards[nonJokerIdx].Rank - (nonJokerIdx - i);
                                    else
                                        throw new RummyException("Rank of joker card could not be figured out! This should never happen!");
                                }
                            }
                        }
                        else if (rank == Card.CardRank.ACE && i == 0)
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
            {
                cards.Add(card);
            }
            card.transform.SetParent(transform, true);
            card.transform.localScale = card.transform.localScale * CardScale;
            Objects = new List<Card>(cards);
            UpdatePositions();
        }

        public void RemoveCard(Card card)
        {
            Objects.Remove(card);
            card.transform.localScale = card.transform.localScale / CardScale;
            card.transform.SetParent(null, true);
            UpdatePositions();
        }

        public bool CanFit(Card newCard, out Card Joker)
        {
            Joker = null;
            switch (Type)
            {
                case SpotType.NONE: return false;
                case SpotType.SET:
                    Set set = new Set(Objects);
                    if (!newCard.IsJoker())
                    {
                        if (newCard.Rank != set.Rank)
                            return false;

                        Card joker = Objects.FirstOrDefault(c => c.IsJoker());
                        if (joker != null)
                        {
                            //Allow adding a third card of one color if one of the other two is a joker
                            if (set.HasTwoBlackCards && joker.IsRed() && newCard.IsBlack())
                                return false;
                            if (set.HasTwoRedCards && joker.IsBlack() && newCard.IsRed())
                                return false;

                            if (joker.Color == newCard.Color)
                                Joker = joker;

                            //Otherwise, only allow adding a card whose suit is not already there
                            var nonJokers = Objects.Where(c => !c.IsJoker());
                            return nonJokers.All(c => c.Suit != newCard.Suit);
                        }
                        else
                            return Objects.All(c => c.Suit != newCard.Suit);
                    }
                    else
                    {
                        //Don't allow more than one joker
                        if (Objects.Any(c => c.IsJoker()))
                            return false;

                        if (newCard.IsBlack() && set.HasTwoBlackCards)
                            return false;
                        if (newCard.IsRed() && set.HasTwoRedCards)
                            return false;
                        return true;
                    }
                default: //SpotType.RUN:
                    Run run = new Run(Objects);
                    if (!newCard.IsJoker())
                    {
                        if (newCard.Suit != run.Suit)
                            return false;

                        var jokers = Objects.Where(c => c.IsJoker());
                        Card replacedJoker = null;

                        foreach (var joker in jokers)
                        {
                            Card.CardRank actualJokerRank = Card.CardRank.JOKER;
                            int jokerIdx = Objects.IndexOf(joker);
                            int higherNonJokerIdx = CardUtil.GetFirstNonJokerCardIdx(Objects, jokerIdx + 1, true);
                            if (higherNonJokerIdx != -1)
                                actualJokerRank = Objects[higherNonJokerIdx].Rank - (higherNonJokerIdx - jokerIdx);
                            else
                            {
                                int lowerNonJokerIdx = CardUtil.GetFirstNonJokerCardIdx(Objects, jokerIdx - 1, false);
                                if (lowerNonJokerIdx != -1)
                                    actualJokerRank = Objects[lowerNonJokerIdx].Rank + (jokerIdx - lowerNonJokerIdx);
                                else
                                    throw new RummyException("Rank of joker card could not be figured out! This should never happen!");
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

                        return (newCard.Rank == run.HighestRank + 1 && run.HighestRank != Card.CardRank.ACE) ||
                                (newCard.Rank == run.LowestRank - 1 && run.LowestRank != Card.CardRank.ACE) ||
                                (newCard.Rank == Card.CardRank.ACE && run.LowestRank == Card.CardRank.TWO);
                    }
                    else
                    {
                        return newCard.Color == run.Color && (run.HighestRank != Card.CardRank.ACE || run.LowestRank != Card.CardRank.ACE);
                    }
            }
        }
    }

}