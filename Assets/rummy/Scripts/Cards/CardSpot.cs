﻿using System.Linq;
using rummy.Utility;
using UnityEngine;

namespace rummy.Cards
{
    public class CardSpot : HandCardSpot
    {
        public override string ToString() => gameObject.name;

        public enum SpotType
        {
            NONE,
            RUN,
            SET
        }
        public SpotType Type;

        protected override void InitValues()
        {
            zIncrement = -0.01f;
        }

        protected override float GetDeltaAngle()
        {
            return angleSpread / (Mathf.Max(Objects.Count, 4) - 1);
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
        /// Return whether this CardSpot is full and cannot take any more cards
        /// </summary>
        public bool IsFull(bool includeJokers)
        {
            if (Type == SpotType.NONE)
                return false;

            int count = Objects.Count(c => !c.IsJoker() || includeJokers);
            if (Type == SpotType.RUN)
                return count == 14;
            // else SpotType.SET
            return count == 4;
        }

        public override void AddCard(Card card)
        {
            // By default, add the new card at the end
            int idx = Objects.Count;

            // If the run is empty or only contains joker cards, also add the new card at the end
            // otherwise
            if (Type == SpotType.RUN && Objects.Count(c => !c.IsJoker()) > 0)
            {
                // Find out the rank of the last card in the run
                int highestNonJokerIdx = Objects.GetFirstCardIndex(Objects.Count - 1, false);
                var highestNonJokerRank = Objects[highestNonJokerIdx].Rank;
                var highestRank = highestNonJokerRank + (Objects.Count - 1 - highestNonJokerIdx);
                if (highestNonJokerIdx == 0 && highestNonJokerRank == Card.CardRank.ACE)
                    highestRank = (Card.CardRank)Objects.Count;

                // If adding ACE after King is not possible, add ACE at beginning
                if (card.Rank == Card.CardRank.ACE &&
                    (highestRank < Card.CardRank.KING ||
                    (highestRank == Card.CardRank.ACE && !Objects[^1].IsJoker())))
                {
                    idx = 0;
                }
                else if (card.IsJoker()) // Joker will be added at the end, if possible
                {
                    if (highestRank == Card.CardRank.ACE)
                        idx = 0;
                    else
                        idx = Objects.Count;
                }
                else // Any other case, the card will be sorted by rank
                {
                    for (int i = 0; i < Objects.Count; i++)
                    {
                        var rank = Objects[i].Rank;
                        if (Objects[i].IsJoker()) // Figure out the rank of the card which the joker is replacing
                        {
                            if (i == 0 && Objects.Count == 1)
                            {
                                // Joker is the only card in the run and the next card comes after the joker
                                idx = 1;
                                break;
                            }
                            rank = CardUtil.GetJokerRank(Objects, i);
                        }
                        else if (i == 0 && rank == Card.CardRank.ACE)
                            rank = (Card.CardRank)1; // Although it's not actually a Joker

                        if (rank > card.Rank)
                        {
                            idx = i;
                            break;
                        }
                    }
                }
            }
            AddCard(card, idx);
        }

        /// <summary>
        /// Check whether this CardSpot can fit the 'newCard', optionally
        /// returning the Joker which is currently occupying that spot
        /// </summary>
        /// <returns>True if the card can fit in this spot, false otherwise</returns>
        public bool CanFit(Card newCard, out Card Joker)
        {
            Joker = null;
            if (Type == SpotType.NONE)
                return false;
            if (Type == SpotType.SET)
                return new Set(Objects).CanFit(newCard, out Joker);
            return new Run(Objects).CanFit(newCard, out Joker);
        }
    }

}