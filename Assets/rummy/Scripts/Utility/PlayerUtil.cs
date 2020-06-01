using System.Linq;
using System.Collections.Generic;
using rummy.Cards;

namespace rummy.Utility
{

    public static class PlayerUtil
    {
        /// <summary>
        /// Returns the optimal card in 'PlayerHandCards' to discard.
        /// There will always be a valid card returned, no need for null checks
        /// </summary>
        public static Card GetCardToDiscard(List<Card> PlayerHandCards, List<Single> singleLayDownCards, bool HasLaidDown, out List<string> thoughts)
        {
            thoughts = new List<string>();
            var possibleDiscards = new List<Card>(PlayerHandCards);

            //Try to keep the best combo and known singles on hand for laying down later
            if (!HasLaidDown)
                possibleDiscards = KeepUsableCards(PlayerHandCards, singleLayDownCards, out thoughts);

            //Don't allow discarding joker cards unless they're all that's left
            var jokerCards = possibleDiscards.Where(c => c.IsJoker());
            if (possibleDiscards.Count == jokerCards.Count())
                return jokerCards.First();

            possibleDiscards = possibleDiscards.Except(jokerCards).ToList();

            //Check for duo sets&runs in the remaining cards
            //and keep them on hand, if possible (only for >3 cards, because
            //keeping a duo and discarding the third card makes no sense)
            if (possibleDiscards.Count > 3)
            {
                var laidDownCards = Tb.I.GameMaster.GetAllCardSpotCards();
                var duos = CardUtil.GetAllDuoSets(possibleDiscards, laidDownCards);

                duos.AddRange(CardUtil.GetAllDuoRuns(possibleDiscards, laidDownCards));

                // Keep high value duos if the player has yet to lay down any cards
                duos = (HasLaidDown ? duos.OrderBy(duo => duo.Value()) : duos.OrderByDescending(duo => duo.Value())).ToList();

                var keptDuos = new List<Duo>();
                var notKeptDuos = new List<Duo>();
                foreach (var duo in duos)
                {
                    if (possibleDiscards.Except(duo.GetList()).Count() >= 1)
                    {
                        possibleDiscards.Remove(duo.A);
                        possibleDiscards.Remove(duo.B);
                        keptDuos.Add(duo);
                    }
                    else
                        notKeptDuos.Add(duo);
                }
                if (notKeptDuos.Any())
                    thoughts.Add(notKeptDuos.GetListMsg("Cannot keep duo"));
                if (keptDuos.Any())
                    thoughts.Add(keptDuos.GetListMsg("Keep duo"));
            }
            //Discard the card with the highest value
            return possibleDiscards.OrderByDescending(c => c.Value).First();
        }

        /// <summary>
        /// Returns a list of cards in 'PlayerHandCards' which can be discarded.
        /// It always contains 1 or more cards.
        /// </summary>
        public static List<Card> KeepUsableCards(List<Card> PlayerHandCards, List<Single> singleLayDownCards, out List<string> thoughts)
        {
            thoughts = new List<string>();

            var possibleDiscards = new List<Card>(PlayerHandCards);
            var laidDownCards = Tb.I.GameMaster.GetAllCardSpotCards();
            var combos = CardUtil.GetAllPossibleCombos(PlayerHandCards, laidDownCards, false);

            // Don't discard the cards of the best combo
            if (combos.Count > 0)
                possibleDiscards = possibleDiscards.Except(combos[0].GetCards()).ToList();

            if (possibleDiscards.Count == 0)
            {
                thoughts.Add("Cannot keep all cards of the best combo for later");
                var card = GetLeastImpactfulCard(PlayerHandCards);
                thoughts.Add("Discard " + card + " for best outcome");
                return new List<Card>() { card };
            }

            // Keep single cards which can be used later
            if (possibleDiscards.Count > 1)
            {
                var singleCards = singleLayDownCards.Select(s => s.Card).ToList();
                possibleDiscards = possibleDiscards.Except(singleCards).ToList();

                // Saving all single cards is not possible. Discard the one with the highest value
                if (possibleDiscards.Count == 0)
                {
                    var keptSingle = singleCards.OrderByDescending(c => c.Value).FirstOrDefault();
                    possibleDiscards.Add(keptSingle);
                    singleCards.Remove(keptSingle);
                }

                if (singleCards.Any())
                    thoughts.Add(singleCards.GetListMsg("Keep single"));
            }
            return possibleDiscards;
        }

        /// <summary>
        /// Returns the highest value card which would lead to the highest possible hand card combo when excluded from building combos
        /// </summary>
        public static Card GetLeastImpactfulCard(List<Card> PlayerHandCards)
        {
            int maxValueCombo = 0;
            Card cardToRemove = null;

            foreach (Card card in PlayerHandCards)
            {
                var hypotheticalHandCards = new List<Card>(PlayerHandCards);
                hypotheticalHandCards.Remove(card);

                var possibleCombos = CardUtil.GetAllPossibleCombos(hypotheticalHandCards, Tb.I.GameMaster.GetAllCardSpotCards(), true);
                int hypotheticalValue = possibleCombos.Any() ? possibleCombos[0].Value : 0;

                if (hypotheticalValue == 0)
                    continue;

                if (hypotheticalValue > maxValueCombo)
                {
                    maxValueCombo = hypotheticalValue;
                    cardToRemove = card;
                }
                else if (hypotheticalValue == maxValueCombo)
                {
                    if (card.Value > cardToRemove.Value)
                        cardToRemove = card;
                }
            }
            return cardToRemove;
        }

        /// <summary>
        /// Updates the list of single cards which can be laid down, <see cref="singleLayDownCards"/>.
        /// </summary>
        /// <param name="turnEnded">Whether the update happens after the player's turn has ended and a card was already discarded</param>
        public static List<Single> UpdateSingleLaydownCards(List<Card> PlayerHandCards, CardCombo laydownCards, bool turnEnded = false)
        {
            var singleLayDownCards = new List<Single>();

            var availableCards = new List<Card>(PlayerHandCards);

            //Don't check cards part of sets or runs
            availableCards = availableCards.Except(laydownCards.GetCards()).ToList();

            var jokerCards = availableCards.Where(c => c.IsJoker());
            bool allowedJokers = false;

            //At first, do not allow jokers to be laid down as singles
            availableCards = availableCards.Where(c => !c.IsJoker()).ToList();

            bool canFitCard = false;
            do
            {
                var cardSpots = Tb.I.GameMaster.GetAllCardSpots().Where(cs => !cs.IsFull(false));
                canFitCard = false;
                foreach (CardSpot cardSpot in cardSpots)
                {
                    for (int i = availableCards.Count - 1; i >= 0; i--)
                    {
                        Card availableCard = availableCards[i];
                        if (!cardSpot.CanFit(availableCard, out Card joker))
                            continue;

                        //Find all single cards which are already gonna be added to the cardspot in question
                        var plannedMoves = singleLayDownCards.Where(single => single.CardSpot == cardSpot);
                        bool alreadyPlanned = false;
                        foreach (var move in plannedMoves)
                        {
                            //Don't add the current card if another is already planned for that place
                            if (move.Card.Suit == availableCard.Suit && move.Card.Rank == availableCard.Rank)
                            {
                                alreadyPlanned = true;
                                break;
                            }
                        }
                        if (alreadyPlanned)
                            continue;

                        var newEntry = new Single(availableCard, cardSpot, joker);
                        singleLayDownCards.Add(newEntry);
                        availableCards.RemoveAt(i);
                        canFitCard = true;
                    }
                }

                //Allow laying down single jokers if no other single card can be found
                //less than 3 normal cards remain and the turn has not yet ended
                if (!canFitCard && !allowedJokers && jokerCards.Count() > 0 && availableCards.Count < 3 && !turnEnded)
                {
                    availableCards.AddRange(jokerCards);
                    allowedJokers = true;
                    canFitCard = true;
                }
            } while (canFitCard);

            return singleLayDownCards;
        }
    }


}