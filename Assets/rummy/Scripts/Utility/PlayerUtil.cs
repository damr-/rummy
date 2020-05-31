using System.Linq;
using System.Collections.Generic;
using rummy.Cards;

namespace rummy.Utility
{

    public static class PlayerUtil
    {
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
        /// Removes all the cards from 'PlayerHandCards' which are part of a finished set/run
        /// or which are going to be laid down as single card later, if possible.
        /// Returns a list of cards which can be discarded
        /// </summary>
        public static List<Card> KeepUsableCards(List<Card> PlayerHandCards, List<Single> singleLayDownCards, out List<string> thought)
        {
            var newPossibleDiscards = new List<Card>(PlayerHandCards);
            var sets = CardUtil.GetPossibleSets(PlayerHandCards);
            var runs = CardUtil.GetPossibleRuns(PlayerHandCards);
            foreach (var set in sets)
                newPossibleDiscards = newPossibleDiscards.Except(set.Cards).ToList();
            foreach (var run in runs)
                newPossibleDiscards = newPossibleDiscards.Except(run.Cards).ToList();
            var laidDownCards = Tb.I.GameMaster.GetAllCardSpotCards();
            var jokerSets = CardUtil.GetPossibleJokerSets(PlayerHandCards, laidDownCards);
            var jokerRuns = CardUtil.GetPossibleJokerRuns(PlayerHandCards, laidDownCards);
            foreach (var jokerSet in jokerSets)
                newPossibleDiscards = newPossibleDiscards.Except(jokerSet.Cards).ToList();
            foreach (var jokerRun in jokerRuns)
                newPossibleDiscards = newPossibleDiscards.Except(jokerRun.Cards).ToList();

            thought = new List<string>() { "Cannot keep all cards for later" };
            // Discard the card with the highest value and least impact on combo value
            if (newPossibleDiscards.Count == 0)
            {
                var card = GetLeastImpactfulCard(PlayerHandCards);
                thought.Add("Discarding " + card + " without destroying the most valued combo");
                return new List<Card>() { card };
            }

            // Keep single cards which can be used later
            if (newPossibleDiscards.Count > 1)
            {
                var singleCards = singleLayDownCards.Select(s => s.Card).ToList();
                newPossibleDiscards = newPossibleDiscards.Except(singleCards).ToList();

                // Saving all single cards is not possible. Discard the one with the highest value
                if (newPossibleDiscards.Count == 0)
                {
                    var keptSingle = singleCards.OrderByDescending(c => c.Value).FirstOrDefault();
                    newPossibleDiscards.Add(keptSingle);
                    singleCards.Remove(keptSingle);
                }

                if (singleCards.Any())
                {
                    string msg = "";
                    singleCards.ForEach(s => msg += s + ", ");
                    thought.Add("Keeping singles " + msg.TrimEnd().TrimEnd(','));
                }
            }
            return newPossibleDiscards;
        }

        public static Card GetCardToDiscard(List<Card> PlayerHandCards, List<Single> singleLayDownCards, bool HasLaidDown, out List<string> thoughts)
        {
            thoughts = new List<string>();

            var possibleDiscards = new List<Card>(PlayerHandCards);

            //Keep possible runs/sets/singles on hand for laying down later
            if (!HasLaidDown)
                possibleDiscards = KeepUsableCards(PlayerHandCards, singleLayDownCards, out thoughts);

            //At first, don't allow discarding joker cards
            var jokerCards = possibleDiscards.Where(c => c.IsJoker());
            possibleDiscards = possibleDiscards.Except(jokerCards).ToList();

            //Check for duo sets&runs and keep them on hand, if possible.
            //Only check if there are more than 3 cards on the player's hand, because
            //keeping a duo and discarding the third card makes no sense.
            if (possibleDiscards.Count > 3)
            {
                var laidDownCards = Tb.I.GameMaster.GetAllCardSpotCards();
                var duos = CardUtil.GetAllDuoSets(PlayerHandCards, laidDownCards);
                duos.AddRange(CardUtil.GetAllDuoRuns(PlayerHandCards, laidDownCards));

                var eligibleDuos = new List<Duo>();
                foreach (var duo in duos)
                {
                    if (possibleDiscards.Contains(duo.A) && possibleDiscards.Contains(duo.B))
                        eligibleDuos.Add(duo);
                }
                duos = eligibleDuos.OrderByDescending(duo => duo.A.Value + duo.B.Value).ToList();

                var keptDuos = new List<Duo>();
                foreach (var duo in duos)
                {
                    if (possibleDiscards.Except(duo.GetList()).Count() >= 1)
                    {
                        possibleDiscards.Remove(duo.A);
                        possibleDiscards.Remove(duo.B);
                        keptDuos.Add(duo);
                    }
                }
                if (keptDuos.Any())
                {
                    string msg = "";
                    keptDuos.ForEach(duo => msg += duo.ToString() + ", ");
                    thoughts.Add("Duo" + (keptDuos.Count > 1 ? "s " : " ") + msg.TrimEnd().TrimEnd(','));
                }
            }

            //If all the remaining cards are joker cards, allow discarding them
            if (possibleDiscards.Count == 0)
                possibleDiscards.AddRange(jokerCards);

            //Discard the card with the highest value
            Card card = possibleDiscards.OrderByDescending(c => c.Value).FirstOrDefault();
            return card;
        }

        /// <summary>
        /// Updates the list of single cards which can be laid down, <see cref="singleLayDownCards"/>.
        /// </summary>
        public static List<Single> UpdateSingleLaydownCards(List<Card> PlayerHandCards, CardCombo laydownCards)
        {
            var singleLayDownCards = new List<Single>();

            //Don't check cards part of sets or runs
            var availableCards = new List<Card>(PlayerHandCards);
            foreach (var set in laydownCards.Sets)
                availableCards = availableCards.Except(set.Cards).ToList();
            foreach (var run in laydownCards.Runs)
                availableCards = availableCards.Except(run.Cards).ToList();

            var jokerCards = availableCards.Where(c => c.IsJoker());
            bool allowedJokers = false;

            //At first, do not allow jokers to be laid down as singles
            availableCards = availableCards.Where(c => !c.IsJoker()).ToList();

            bool canFitCard = false;
            do
            {
                var cardSpots = Tb.I.GameMaster.GetAllCardSpots();
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
                            //If a card with the same rank and suit is already planned to be added to cardspot
                            //the current card cannot be added anymore
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
                //and and the player has less than 3 cards on their hand
                if (!canFitCard && !allowedJokers && jokerCards.Count() > 0 && availableCards.Count < 3)
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