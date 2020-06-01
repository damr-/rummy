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
        public override string ToString() => gameObject.name;

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

        public List<Card> ResetSpot()
        {
            Type = SpotType.NONE;

            var cards = new List<Card>(Objects);
            while (Objects.Count > 0)
                RemoveCard(Objects[0]);
            return cards;
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
                // Joker is worth 20 on hand
                foreach (var card in Objects)
                    value += card.IsJoker() ? 20 : card.Value;
            }
            return value;
        }

        /// <summary>
        /// Returns whether this CardSpot is full and cannot take any more cards.
        /// </summary>
        public bool IsFull(bool includeJokers)
        {
            if (Type == SpotType.NONE)
                return false;

            int count = Objects.Count(c => !c.IsJoker() || includeJokers);
            if (Type == SpotType.RUN)
                return count == 14;
            //else Type is SpotType.SET
            return count == 4;
        }

        public void AddCard(Card card)
        {
            var cards = new List<Card>(Objects);

            //By default, add the new card at the end
            int idx = cards.Count;

            //If the run is empty or only contains joker cards, add the new card at the end
            //otherwise
            if (Type == SpotType.RUN && cards.Count(c => !c.IsJoker()) > 0)
            {
                //Find out the rank of the last card in the run
                int highestNonJokerIdx = cards.GetFirstCardIndex(cards.Count - 1, false);
                var highestNonJokerRank = cards[highestNonJokerIdx].Rank;
                var highestRank = highestNonJokerRank + (cards.Count - 1 - highestNonJokerIdx);
                if (highestNonJokerIdx == 0 && highestNonJokerRank == Card.CardRank.ACE)
                    highestRank = (Card.CardRank)cards.Count;

                // If adding ACE after King is not possible, add ACE at beginning
                if (card.Rank == Card.CardRank.ACE &&
                    highestRank != Card.CardRank.KING &&
                    highestNonJokerRank != Card.CardRank.KING)
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
                            if (i == 0 && cards.Count == 1)
                            {
                                //Joker is the only card in the run and the next card comes after the joker
                                idx = 1;
                                break;
                            }
                            rank = CardUtil.GetJokerRank(cards, i);
                        }
                        else if (i == 0 && rank == Card.CardRank.ACE)
                            rank = (Card.CardRank)1; //Although it's not actually a Joker

                        if (rank > card.Rank)
                        {
                            idx = i;
                            break;
                        }
                    }
                }
            }

            cards.Insert(idx, card);
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

        /// <summary>
        /// Checks whether this CardSpot can fit the 'newCard', optionally
        /// returning the Joker which is currently occupying that spot
        /// </summary>
        /// <returns>True if the card can fit in this spot, false otherwise</returns>
        public bool CanFit(Card newCard, out Card Joker)
        {
            Joker = null;
            if (Type == SpotType.NONE) return false;
            if (Type == SpotType.SET)
            {
                if (IsFull(false))
                    return false;

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
                    if (newCard.IsBlack() && set.HasTwoBlackCards)
                        return false;
                    if (newCard.IsRed() && set.HasTwoRedCards)
                        return false;
                    return true;
                }
            }

            // Type == SpotType.RUN
            Run run = new Run(Objects);
            if (!newCard.IsJoker())
            {
                if (newCard.Suit != run.Suit || IsFull(false))
                    return false;

                // Check whether the new card replaces a joker
                var jokers = Objects.Where(c => c.IsJoker());
                Card replacedJoker = null;
                foreach (var joker in jokers)
                {
                    Card.CardRank jokerRank = CardUtil.GetJokerRank(Objects, Objects.IndexOf(joker));
                    if (jokerRank == newCard.Rank)
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