using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using rummy.Cards;
using rummy.Utility;

namespace rummy
{

    public class AIPlayer : Player
    {
        public bool enableThoughts = false;

        private float waitStartTime;

        #region Events
        public class Event_NewThought : UnityEvent<string> { }
        public Event_NewThought NewThought = new();
        private void EmitThought(string thought)
        {
            if (enableThoughts)
                NewThought.Invoke(thought);
        }
        #endregion

        protected override void Update()
        {
            if (State == PlayerState.WAITING && Time.time - waitStartTime > Tb.I.GameMaster.PlayWaitDuration)
            {
                State = PlayerState.PLAYING;
                if (!Tb.I.GameMaster.LayingAllowed() || !HasLaidDown)
                {
                    DiscardCard();
                }
                else
                {
                    State = PlayerState.LAYING;
                    isCardBeingLaidDown = false;
                    currentMeldIdx = 0;
                    currentCardIdx = 0;
                    currentCardSpot = null;
                    layStage = LayStage.SETS;

                    if (laydownCardCombo.Sets.Count == 0)
                    {
                        layStage = LayStage.RUNS;
                        if (laydownCardCombo.Runs.Count == 0)
                            LayingCardsDone();
                    }
                }
            }

            base.Update();
        }

        public override void ResetPlayer()
        {
            EmitThought("<CLEAR>");
            base.ResetPlayer();
        }

        public override void BeginTurn()
        {
            EmitThought("<CLEAR>");
            base.BeginTurn();
        }

        protected override Card GetCardToDraw()
        {
            bool takeFromDiscardStack = false;
            
            // Check if we want to draw from discard stack
            if (HasLaidDown)
            {
                Card discardedCard = Tb.I.DiscardStack.TopmostCard();
                
                // Check whether the card would enable a combo
                var hypotheticalHandCards = new List<Card>(HandCardSpot.Objects) { discardedCard };
                var hypotheticalBestCombo = GetBestCardCombo(hypotheticalHandCards, false);
                int hypotheticalValue = hypotheticalBestCombo.Value;

                int currentValue = GetBestCardCombo(HandCardSpot.Objects, false).Value;

                if (hypotheticalValue > currentValue)
                {
                    EmitThought($"Take {discardedCard.ToRichString()} from discard pile to finish {hypotheticalBestCombo}");
                    takeFromDiscardStack = true;
                }
                else
                {
                    // Alternatively, check if the last player overlooked using it as a Single card (AI never do that, but the human might)
                    var cardSpots = Tb.I.GameMaster.GetAllCardSpots().Where(cs => !cs.IsFull(false));
                    foreach (var cardSpot in cardSpots)
                    {
                        Card joker = null;
                        List<int> spots = new();
                        if (cardSpot.CanFit(discardedCard, out joker, out spots))
                        {
                            EmitThought($"Take {discardedCard.ToRichString()} from discard pile as Single for {cardSpot}");
                            takeFromDiscardStack = true;
                        }
                    }
                }
            }

            Card card;
            if (takeFromDiscardStack)
                card = Tb.I.DiscardStack.DrawCard();
            else
                card = Tb.I.CardStack.DrawCard();
            return card;
        }

        protected override void DrawCardMoveFinished(Card card, bool isServingCard)
        {
            base.DrawCardMoveFinished(card, isServingCard);

            if (isServingCard)
                return;

            var combos = UpdatePossibleCombos();
            laydownCardCombo = combos.Count > 0 ? combos[0] : new CardCombo();
            UpdatePossibleSingles(laydownCardCombo);

            if (Tb.I.GameMaster.LayingAllowed())
            {
                var usedJokers = false;

                // If the player has not laid down melds yet, check if their sum would be enough to do so
                if (!HasLaidDown)
                {
                    HasLaidDown = laydownCardCombo.Value >= Tb.I.GameMaster.MinimumLaySum;

                    /// Try to reach <see cref="GameMaster.MinimumLaySum"/> by appending jokers to any possible cardcombo
                    var jokers = HandCardSpot.Objects.Where(c => c.IsJoker()).ToList();
                    if (!HasLaidDown && jokers.Count() > 0)
                    {
                        for (int i = 0; i < combos.Count; i++)
                        {
                            var combo = new CardCombo(combos[i]);
                            var jokersInUse = combo.GetCards().Where(c => c.IsJoker()).ToList();
                            var remainingJokers = jokers.Except(jokersInUse).ToList();
                            if (remainingJokers.Count == 0)
                                continue;
                            var canLayCombo = combo.TryAddJoker(remainingJokers);
                            if (canLayCombo && combo.CardCount < HandCardCount)
                            {
                                usedJokers = true;
                                laydownCardCombo = combo;
                                combos.Insert(0, combo);
                                PossibleCardCombosChanged.Invoke(combos);
                                EmitThought("Use jokers to lay down");
                                HasLaidDown = true;
                                break;
                            }
                        }
                        if (!HasLaidDown)
                            EmitThought($"Cannot reach {Tb.I.GameMaster.MinimumLaySum} using jokers");
                    }
                }

                // At least one card must remain when laying down
                if (!usedJokers && HasLaidDown && laydownCardCombo.CardCount == HandCardCount)
                    KeepOneSingleCard();
            }

            State = PlayerState.WAITING;
            waitStartTime = Time.time;
        }

        /// <summary>
        /// Return the card combo with the highest possible value from the given 'HandCards'.
        /// </summary>
        /// <param name="HandCards">The cards in the player's hand</param>
        /// <param name="broadcastCombos">Whether to broadcast all possible combos for UI output</param>
        /// <returns>The combo with the highest value or an empty one, if none was found</returns>
        private CardCombo GetBestCardCombo(List<Card> HandCards, bool broadcastCombos)
        {
            var possibleCombos = CardUtil.GetAllPossibleCombos(HandCards, Tb.I.GameMaster.GetAllCardSpotCards(), false);
            if (broadcastCombos)
                PossibleCardCombosChanged.Invoke(possibleCombos);
            return possibleCombos.Count > 0 ? possibleCombos[0] : new CardCombo();
        }

        protected override void ReturnJokerMoveFinished(Card joker)
        {
            base.ReturnJokerMoveFinished(joker);

            // All possible runs/sets/singles have to be calculated again with that new joker
            laydownCardCombo = GetBestCardCombo(HandCardSpot.Objects, true);
            UpdatePossibleSingles(laydownCardCombo);

            if (laydownCardCombo.CardCount == HandCardCount)
                KeepOneSingleCard();

            State = PlayerState.WAITING;
            waitStartTime = Time.time;
        }

        protected override void LayingCardsDone()
        {
            // With only one card left, just end the game
            if (HandCardCount == 1)
            {
                DiscardCard();
                return;
            }

            // Check if there are any (more) single cards to lay down
            UpdatePossibleSingles(laydownCardCombo);

            if (laydownSingles.Count == HandCardCount)
                KeepOneSingleCard();

            if (laydownSingles.Count > 0)
            {
                currentCardIdx = 0;
                layStage = LayStage.SINGLES;
            }
            else
                DiscardCard();
        }

        private void UpdatePossibleSingles(CardCombo laydownCards, bool turnEnded = false)
        {
            laydownSingles = PlayerUtil.UpdateSingleLaydownCards(HandCardSpot.Objects, laydownCards, turnEnded);
            PossibleSinglesChanged.Invoke(laydownSingles);
        }

        /// <summary>
        /// Choose one card in <see cref="singleLayDownCards"/> which is kept on hand. Prioritize cards which do not replace a joker
        /// </summary>
        private void KeepOneSingleCard()
        {
            Single keptSingle = laydownSingles.FirstOrDefault(c => c.Joker == null);
            if (keptSingle == null)
                keptSingle = laydownSingles[0];
            laydownSingles.Remove(keptSingle);
            EmitThought($"Keep single {keptSingle.Card.ToRichString()}");
        }

        protected void DiscardCard()
        {
            State = PlayerState.DISCARDING;

            var thoughts = new List<string>();
            Card card = PlayerUtil.GetCardToDiscard(HandCardSpot.Objects, laydownSingles, HasLaidDown, ref thoughts);
            thoughts.ForEach(t => EmitThought(t));

            DiscardCard(card);
        }

        protected override void DiscardCardMoveFinished(Card card)
        {
            // Refresh the list of possible card combos and singles for the UI
            GetBestCardCombo(HandCardSpot.Objects, true);
            UpdatePossibleSingles(laydownCardCombo, true);
            base.DiscardCardMoveFinished(card);
        }
    }

}