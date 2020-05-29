using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using rummy.Cards;
using rummy.Utility;
using Single = rummy.Cards.Single;

namespace rummy
{

    public class Player : MonoBehaviour
    {
        private float waitStartTime;

        public enum PlayerState
        {
            IDLE = 0,
            DRAWING = 1,
            WAITING = 2,
            PLAYING = 3,
            LAYING = 4,
            RETURNING_JOKER = 5,
            DISCARDING = 6
        }
        public PlayerState playerState { get; private set; } = PlayerState.IDLE;

        #region PlayerCardSpot
        private CardSpot HandCardSpot;
        public int PlayerCardCount { get { return HandCardSpot.Cards.Count; } }
        public int PlayerHandValue { get { return HandCardSpot.GetValue(); } }

        public Transform PlayerCardSpotsParent;
        public List<CardSpot> GetPlayerCardSpots()
        {
            if (playerCardSpots.Count == 0)
                playerCardSpots = PlayerCardSpotsParent.GetComponentsInChildren<CardSpot>().ToList();
            return playerCardSpots;
        }
        private List<CardSpot> playerCardSpots = new List<CardSpot>();
        private CardSpot currentCardSpot;
        public int GetLaidCardsSum() => GetPlayerCardSpots().Sum(spot => spot.GetValue());
        #endregion

        #region CardSets
        private CardCombo laydownCards = new CardCombo();
        private List<Single> singleLayDownCards = new List<Single>();
        private int currentCardPackIdx = 0, currentCardIdx = 0;
        private Card returningJoker;
        private bool isCardBeingLaidDown, isJokerBeingReturned;

        private enum LayStage
        {
            SETS = 0,
            RUNS = 1,
            SINGLES = 2
        }
        private LayStage layStage = LayStage.SETS;

        /// <summary>
        /// Whether the player can lay down cards and pick up cards from the discard stack
        /// </summary>
        public bool HasLaidDown { get; private set; }
        #endregion

        #region Observables
        public class Event_TurnFinished : UnityEvent<Player> { }
        public UnityEvent TurnFinished = new UnityEvent();

        public class Event_PossibleCardCombosChanged : UnityEvent<List<CardCombo>> { }
        public Event_PossibleCardCombosChanged PossibleCardCombosChanged = new Event_PossibleCardCombosChanged();

        public class Event_PossibleSinglesChanged : UnityEvent<List<Single>> { }
        public Event_PossibleSinglesChanged PossibleSinglesChanged = new Event_PossibleSinglesChanged();

        public class Event_NewThought : UnityEvent<string> { }
        public Event_NewThought NewThought = new Event_NewThought();
        #endregion

        private void AddCard(Card card)
        {
            HandCardSpot.AddCard(card);
            card.SetVisible(true);
        }

        public void ResetPlayer()
        {
            HasLaidDown = false;
            playerState = PlayerState.IDLE;
            GetPlayerCardSpots().ForEach(spot => spot.ResetSpot());
            HandCardSpot.ResetSpot();
            PossibleCardCombosChanged.Invoke(new List<CardCombo>());
            PossibleSinglesChanged.Invoke(new List<Single>());
            NewThought.Invoke("<CLEAR>");
        }

        private void Start()
        {
            if (PlayerCardSpotsParent == null)
                throw new MissingReferenceException("Missing 'PlayerCardSpotsParent' reference for " + gameObject.name);
            HandCardSpot = GetComponentInChildren<CardSpot>();
        }

        private void Update()
        {
            if (playerState == PlayerState.WAITING && Time.time - waitStartTime > Tb.I.GameMaster.PlayWaitDuration)
            {
                playerState = PlayerState.PLAYING;
                if (Tb.I.GameMaster.RoundCount < Tb.I.GameMaster.EarliestAllowedLaydownRound || !HasLaidDown)
                    DiscardUnusableCard();
                else
                {
                    playerState = PlayerState.LAYING;
                    isCardBeingLaidDown = false;
                    currentCardPackIdx = 0;
                    currentCardIdx = 0;
                    currentCardSpot = null;
                    layStage = LayStage.SETS;

                    if (laydownCards.Sets.Count == 0)
                    {
                        layStage = LayStage.RUNS;
                        if (laydownCards.Runs.Count == 0)
                            LayingCardsDone();
                    }
                }
            }

            if (playerState == PlayerState.LAYING)
            {
                if (isCardBeingLaidDown)
                    return;
                isCardBeingLaidDown = true;

                if (layStage == LayStage.SINGLES)
                {
                    currentCardSpot = singleLayDownCards[currentCardIdx].CardSpot;
                }
                else if (currentCardSpot == null)
                {
                    currentCardSpot = GetEmptyCardSpot();
                    currentCardSpot.Type = (layStage == LayStage.RUNS) ? CardSpot.SpotType.RUN : CardSpot.SpotType.SET;
                }

                Card card;
                switch (layStage)
                {
                    case LayStage.SETS:
                        card = laydownCards.Sets[currentCardPackIdx].Cards[currentCardIdx];
                        break;
                    case LayStage.RUNS:
                        card = laydownCards.Runs[currentCardPackIdx].Cards[currentCardIdx];
                        break;
                    default: //LayStage.SINGLES
                        card = singleLayDownCards[currentCardIdx].Card;
                        break;
                }

                HandCardSpot.RemoveCard(card);
                card.MoveFinished.AddListener(LayDownCardMoveFinished);
                card.MoveCard(currentCardSpot.transform.position, Tb.I.GameMaster.AnimateCardMovement);
            }

            if (playerState == PlayerState.RETURNING_JOKER)
            {
                if (isJokerBeingReturned)
                    return;
                isJokerBeingReturned = true;
                currentCardSpot.RemoveCard(returningJoker);
                returningJoker.MoveFinished.AddListener(ReturnJokerMoveFinished);
                returningJoker.MoveCard(HandCardSpot.transform.position, Tb.I.GameMaster.AnimateCardMovement);
            }
        }

        public void BeginTurn()
        {
            NewThought.Invoke("<CLEAR>");
            DrawCard(false);
        }

        public void DrawCard(bool isServingCard)
        {
            playerState = PlayerState.DRAWING;
            bool takeFromDiscardStack = false;
            if (!isServingCard && HasLaidDown)
            {
                // Check if we want to draw from discard stack
                // Note that every player always makes sure not to discard a card which can be added to an already laid-down card pack.
                // Therefore, no need to check for that case here.
                Card discardedCard = Tb.I.DiscardStack.PeekCard();
                var hypotheticalHandCards = new List<Card>(HandCardSpot.Cards) { discardedCard };
                var hypotheticalBestCombo = GetBestCardCombo(hypotheticalHandCards, false, false, false);
                int hypotheticalValue = hypotheticalBestCombo.Value;

                int currentValue = GetBestCardCombo(HandCardSpot.Cards, false, false, false).Value;

                if (hypotheticalValue > currentValue)
                {
                    NewThought.Invoke("Take " + discardedCard + " from discard pile to finish " + hypotheticalBestCombo);
                    takeFromDiscardStack = true;
                }
            }


            Card card;
            if (takeFromDiscardStack)
                card = Tb.I.DiscardStack.DrawCard();
            else
                card = Tb.I.CardStack.DrawCard();

            card.MoveFinished.AddListener(c => DrawCardFinished(c, isServingCard));
            card.MoveCard(transform.position, Tb.I.GameMaster.AnimateCardMovement);
        }

        private void DrawCardFinished(Card card, bool isServingCard)
        {
            card.MoveFinished.RemoveAllListeners();
            AddCard(card);
            if (isServingCard)
            {
                playerState = PlayerState.IDLE;
                return;
            }

            laydownCards = GetBestCardCombo(HandCardSpot.Cards, false, true, true);
            UpdateSingleLaydownCards();

            if (Tb.I.GameMaster.RoundCount >= Tb.I.GameMaster.EarliestAllowedLaydownRound)
            {
                //If the player has not laid down card packs yet, check if their sum would be enough to do so
                if (!HasLaidDown)
                    HasLaidDown = laydownCards.Value >= Tb.I.GameMaster.MinimumLaySum;

                //At least one card must remain when laying down
                if (HasLaidDown && laydownCards.CardCount == HandCardSpot.Cards.Count)
                    KeepOneCard();
            }

            playerState = PlayerState.WAITING;
            waitStartTime = Time.time;
        }

        /// <summary>
        /// Returns the card combo which yield the highest possible points
        /// <param name="allowLayingAll"> Whether combos are allowed which would require the player to lay down all cards from his hand ('HandCards').
        /// This is usually not useful, unless hypothetical hands are examined, where one card was removed before. </param>
        /// <param name="broadcastPossibleCombos">Whether to broadcast all possible combos for output</param>
        /// <param name="broadcastNonDuos">Whether to broadcast when a duo set/run was NOT kept on hand because all necessary cards have already been laid down</param>
        /// </summary>
        private CardCombo GetBestCardCombo(List<Card> HandCards, bool allowLayingAll, bool broadcastPossibleCombos, bool broadcastNonDuos)
        {
            List<CardCombo> possibleCombos = CardUtil.GetAllPossibleCombos(HandCards, Tb.I.GameMaster.GetAllCardSpotCards(), allowLayingAll, broadcastNonDuos);
            if (broadcastPossibleCombos)
            {
                PossibleCardCombosChanged.Invoke(possibleCombos);
            }
            possibleCombos = possibleCombos.OrderByDescending(c => c.Value).ToList();
            return possibleCombos.Count == 0 ? new CardCombo() : possibleCombos[0];
        }

        private void UpdateSingleLaydownCards()
        {
            singleLayDownCards = new List<Single>();
            bool canFitCard = false;
            var reservedSpots = new List<KeyValuePair<CardSpot, List<Card>>>();

            //Check all cards which will not be laid down anyway as part of a set or a run
            List<Card> availableCards = new List<Card>(HandCardSpot.Cards);
            foreach (var set in laydownCards.Sets)
                availableCards = availableCards.Except(set.Cards).ToList();
            foreach (var run in laydownCards.Runs)
                availableCards = availableCards.Except(run.Cards).ToList();

            var jokerCards = availableCards.Where(c => c.IsJoker());
            bool allowedJokers = false;

            //At first, do not allow jokers to be laid down as singles
            availableCards = availableCards.Where(c => !c.IsJoker()).ToList();

            //Find single cards which fit with already lying cards
            do
            {
                var cardSpots = Tb.I.GameMaster.GetAllCardSpots();

                canFitCard = false;
                foreach (CardSpot cardSpot in cardSpots)
                {
                    foreach (Card card in availableCards)
                    {
                        //If the card is already used elsewhere, skip
                        if (singleLayDownCards.Any(kvp => kvp.Card == card))
                            continue;

                        if (!cardSpot.CanFit(card, out Card Joker))
                            continue;

                        //Find all single cards which are already gonna be added to the cardspot in question
                        var plannedMoves = singleLayDownCards.Where(kvp => kvp.CardSpot == cardSpot).ToList();
                        bool alreadyPlanned = false;
                        foreach (var entry in plannedMoves)
                        {
                            //If a card with the same rank and suit is already planned to be added to cardspot
                            //the current card cannot be added anymore
                            if (entry.Card.Suit == card.Suit && entry.Card.Rank == card.Rank)
                            {
                                alreadyPlanned = true;
                                break;
                            }
                        }
                        if (alreadyPlanned)
                            continue;

                        var newEntry = new Single(card, cardSpot, Joker);
                        singleLayDownCards.Add(newEntry);
                        canFitCard = true;
                    }
                }

                //DO allow laying down jokers if no match can be found 
                //but all but one card remaining are jokers
                if (!canFitCard && !allowedJokers && jokerCards.Count() > 0)
                {
                    List<Card> singles = new List<Card>();
                    foreach (var single in singleLayDownCards)
                        singles.Add(single.Card);

                    //Only one card remains (besides jokers)? - Allow laying jokers to try and win the game
                    if (availableCards.Except(singles).Count() == 1)
                    {
                        availableCards.AddRange(jokerCards);
                        canFitCard = true;
                        allowedJokers = true;
                    }
                }
            } while (canFitCard);

            PossibleSinglesChanged.Invoke(singleLayDownCards);
        }

        /// <summary>
        /// Figures out a way to keep at least one card in the player's hand.
        /// This is needed in the case that the player wants to lay down every single card they have left in their hand.
        /// </summary>
        private void KeepOneCard()
        {
            if (singleLayDownCards.Count > 0)
            {
                var minValSingle = singleLayDownCards.OrderBy(c => c.Card.Value).First();
                singleLayDownCards.Remove(minValSingle);
                NewThought.Invoke("Keeping single " + minValSingle);
                return;
            }

            // No card can be kept without destroying the currently best card combo
            // Find out which kept cards allow for the highest combo of the remaining cards
            var eligibleCards = GetCardsWhichAllowHighestComboWhenRemoved(HandCardSpot.Cards);

            if (eligibleCards.Any())
            {
                // Keep the card with the lowest value out of the possible ones
                var keptCard = eligibleCards.OrderBy(c => c.Value).First();
                var optimalHand = new List<Card>(HandCardSpot.Cards);
                optimalHand.Remove(keptCard);
                laydownCards = GetBestCardCombo(optimalHand, true, false, false);
                NewThought.Invoke("Keeping " + keptCard + ", the rest forms best combo " + laydownCards);
                return;
            }
            else // No card was found. Keep the lowest valued card from the lowest valued set/run with more than 3 cards
            {
                var quadSets = laydownCards.Sets.Where(s => s.Count == 4);
                var quadRuns = laydownCards.Runs.Where(r => r.Count > 3);
                Card keptCard = null;
                Run chosenRun = null;
                Set chosenSet = null;
                if (quadSets.Any())
                {
                    chosenSet = quadSets.OrderBy(set => set.Value).First();
                    keptCard = chosenSet.Cards.First();
                }
                else if (quadRuns.Any())
                {
                    chosenRun = quadRuns.OrderBy(run => run.Value).First();
                    var runCards = chosenRun.Cards;
                    var runCard = runCards.First().Rank != Card.CardRank.ACE ? runCards.First() : runCards[1];
                    if (keptCard == null || runCard.Value < keptCard.Value)
                    {
                        chosenSet = null;
                        keptCard = runCard;
                    }
                }

                if (keptCard != null)
                {
                    if (chosenSet != null)
                        laydownCards.Sets.First(set => set.Equal(chosenSet)).Cards.Remove(keptCard);
                    else
                        laydownCards.Runs.First(run => run.Equal(chosenRun)).Cards.Remove(keptCard);

                    string chosenPack = chosenSet != null ? chosenSet.ToString() : chosenRun.ToString();
                    NewThought.Invoke("Keeping " + keptCard + " and laying down " + chosenPack + " without it.");
                    return;
                }
            }

            //No single card was found. Allow all cards of the set/run with the lowest value
            var minValRun = laydownCards.Runs.OrderBy(run => run.Value).FirstOrDefault();
            var minValSet = laydownCards.Sets.OrderBy(set => set.Value).FirstOrDefault();
            if (minValRun != null && minValSet != null)
            {
                if (minValRun.Value < minValSet.Value)
                    minValSet = null;
                else
                    minValRun = null;
            }
            else if (minValRun != null)
                minValSet = null;
            else if (minValSet != null)
                minValRun = null;
            else
            {
                NewThought.Invoke("No single cards, sets or runs found. This should never happen!");
            }

            if (minValSet != null)
            {
                NewThought.Invoke("Keeping " + minValSet);
                laydownCards.Sets.Remove(minValSet);
            }
            else if (minValRun != null)
            {
                NewThought.Invoke("Keeping " + minValRun);
                laydownCards.Runs.Remove(minValRun);
            }
        }

        /// <summary>
        /// Returns the cards which would lead to the highest possible hand card combo when not included in those combos
        /// </summary>
        private List<Card> GetCardsWhichAllowHighestComboWhenRemoved(List<Card> PlayerHandCards)
        {
            int maxValue = 0;
            var eligibleCards = new List<Card>();
            foreach (Card card in PlayerHandCards)
            {
                var hypotheticalHandCards = new List<Card>(PlayerHandCards);
                hypotheticalHandCards.Remove(card);
                int hypotheticalValue = GetBestCardCombo(hypotheticalHandCards, true, false, false).Value;
                if (hypotheticalValue > maxValue)
                {
                    maxValue = hypotheticalValue;
                    eligibleCards = new List<Card>() { card };
                }
                else if (hypotheticalValue > 0 && hypotheticalValue == maxValue)
                    eligibleCards.Add(card);
            }

            return eligibleCards;
        }

        private void LayDownCardMoveFinished(Card card)
        {
            card.MoveFinished.RemoveAllListeners();
            isCardBeingLaidDown = false;
            currentCardSpot.AddCard(card);

            int cardCount, cardPackCount;
            switch (layStage)
            {
                case LayStage.SETS:
                    cardCount = laydownCards.Sets[currentCardPackIdx].Count;
                    cardPackCount = laydownCards.Sets.Count;
                    break;
                case LayStage.RUNS:
                    cardCount = laydownCards.Runs[currentCardPackIdx].Count;
                    cardPackCount = laydownCards.Runs.Count;
                    break;
                default: //LayStage.SINGLES
                    cardCount = singleLayDownCards.Count;
                    cardPackCount = 1;

                    if (!card.IsJoker())
                    {
                        returningJoker = singleLayDownCards[currentCardIdx].Joker;
                        if (returningJoker != null)
                        {
                            playerState = PlayerState.RETURNING_JOKER;
                            return;
                        }
                    }
                    break;
            }

            if (currentCardIdx < cardCount - 1)
            {
                currentCardIdx++;
                return;
            }

            //All cards of the current pack have been laid down            
            currentCardIdx = 0;
            currentCardPackIdx++;
            currentCardSpot = null;  //Find a new spot for the next pack            

            if (currentCardPackIdx < cardPackCount)
                return;

            //All packs have been laid down
            if (layStage == LayStage.SINGLES ||
                layStage == LayStage.RUNS ||
                (layStage == LayStage.SETS && laydownCards.Runs.Count == 0))
            {
                LayingCardsDone();
            }
            else //Start laying runs otherwise
            {
                currentCardPackIdx = 0;
                layStage = LayStage.RUNS;
            }
        }

        private void ReturnJokerMoveFinished(Card joker)
        {
            returningJoker.MoveFinished.RemoveAllListeners();
            isJokerBeingReturned = false;
            HandCardSpot.AddCard(joker);
            returningJoker = null;

            //All possible runs/sets/singles have to be calculated again with that newly returned joker
            laydownCards = GetBestCardCombo(HandCardSpot.Cards, false, true, false);
            UpdateSingleLaydownCards();

            if (laydownCards.CardCount == HandCardSpot.Cards.Count)
                KeepOneCard();

            //Proceed with waiting
            playerState = PlayerState.WAITING;
            waitStartTime = Time.time;
        }

        private void LayingCardsDone()
        {
            //With only one card left, just end the game
            if (HandCardSpot.Cards.Count == 1)
            {
                DiscardUnusableCard();
                return;
            }

            //Check if there are any (more) singles to lay down
            UpdateSingleLaydownCards();

            if (singleLayDownCards.Count == HandCardSpot.Cards.Count)
                KeepOneCard();

            if (singleLayDownCards.Count > 0)
            {
                currentCardIdx = 0;
                layStage = LayStage.SINGLES;
            }
            else
                DiscardUnusableCard();
        }

        private void DiscardUnusableCard()
        {
            playerState = PlayerState.DISCARDING;

            List<Card> possibleDiscards = new List<Card>(HandCardSpot.Cards);

            //Keep possible runs/sets/singles on hand for laying down later
            if (!HasLaidDown)
                possibleDiscards = KeepUsableCards(possibleDiscards);

            //At first, don't allow discarding joker cards
            var jokerCards = possibleDiscards.Where(c => c.IsJoker());
            possibleDiscards = possibleDiscards.Except(jokerCards).ToList();

            //Check for duo sets&runs and exclude them from discarding, if possible.
            //Only check if there are more than 3 cards on the player's hand, because
            //keeping a duo and discarding the third card makes no sense.
            if (possibleDiscards.Count > 3)
            {
                var laidDownCards = Tb.I.GameMaster.GetAllCardSpotCards();
                var duos = CardUtil.GetAllDuoSets(HandCardSpot.Cards, laidDownCards, !HasLaidDown);     // Only broadcast not keeping duos when 
                duos.AddRange(CardUtil.GetAllDuoRuns(HandCardSpot.Cards, laidDownCards, !HasLaidDown)); // it hasn't been done already after drawing

                var eligibleDuos = new List<List<Card>>();
                foreach (var duo in duos)
                {
                    if (possibleDiscards.Contains(duo[0]) && possibleDiscards.Contains(duo[1]))
                        eligibleDuos.Add(new List<Card> { duo[0], duo[1] });
                }
                duos = eligibleDuos.OrderByDescending(duo => duo[0].Value + duo[1].Value).ToList();

                var keptDuos = new List<List<Card>>();
                foreach (var duo in duos)
                {
                    if (possibleDiscards.Except(duo).Count() >= 1)
                    {
                        possibleDiscards.Remove(duo[0]);
                        possibleDiscards.Remove(duo[1]);
                        keptDuos.Add(duo);
                    }
                }
                if (keptDuos.Any())
                {
                    string msg = "";
                    keptDuos.ForEach(duo => msg += duo[0].ToString() + duo[1] + ", ");
                    NewThought.Invoke("Duo" + (keptDuos.Count > 1 ? "s " : " ") + msg.TrimEnd().TrimEnd(','));
                }
            }

            //If all the remaining cards are joker cards, allow discarding them
            if (possibleDiscards.Count == 0)
                possibleDiscards.AddRange(jokerCards);

            //Discard the card with the highest value
            Card card = possibleDiscards.OrderByDescending(c => c.Value).FirstOrDefault();

            HandCardSpot.RemoveCard(card);
            card.MoveFinished.AddListener(DiscardCardMoveFinished);
            card.MoveCard(Tb.I.DiscardStack.GetNextCardPos(), Tb.I.GameMaster.AnimateCardMovement);
        }

        /// <summary>
        /// Removes all the cards from 'possibleDiscards' which are part of a finished set/run 
        /// or which are going to be laid down as single card later.
        /// </summary>
        private List<Card> KeepUsableCards(List<Card> possibleDiscards)
        {
            List<Card> newPossibleDiscards = new List<Card>(possibleDiscards);
            var sets = CardUtil.GetPossibleSets(HandCardSpot.Cards);
            var runs = CardUtil.GetPossibleRuns(HandCardSpot.Cards);
            var laidDownCards = Tb.I.GameMaster.GetAllCardSpotCards();
            var jokerSets = CardUtil.GetPossibleJokerSets(HandCardSpot.Cards, laidDownCards, sets, runs, false);
            var jokerRuns = CardUtil.GetPossibleJokerRuns(HandCardSpot.Cards, laidDownCards, sets, runs, false);
            foreach (var run in runs)
                newPossibleDiscards = newPossibleDiscards.Except(run.Cards).ToList();
            foreach (var set in sets)
                newPossibleDiscards = newPossibleDiscards.Except(set.Cards).ToList();
            foreach (var jokerSet in jokerSets)
                newPossibleDiscards = newPossibleDiscards.Except(jokerSet.Cards).ToList();
            foreach (var jokerRun in jokerRuns)
                newPossibleDiscards = newPossibleDiscards.Except(jokerRun.Cards).ToList();

            // If the hand only consists of possible sets&runs, try to discard the card 
            // with the highest value which does also not destroy the currently best card combo
            if (newPossibleDiscards.Count == 0)
            {
                NewThought.Invoke("Cannot keep all cards for later.");
                int maxValue = GetBestCardCombo(HandCardSpot.Cards, false, false, false).Value;
                List<Card> eligibleCards = new List<Card>();
                foreach (Card possibleCard in HandCardSpot.Cards)
                {
                    var hypotheticalHandCards = new List<Card>(HandCardSpot.Cards);
                    hypotheticalHandCards.Remove(possibleCard);
                    if (GetBestCardCombo(hypotheticalHandCards, true, false, false).Value == maxValue)
                        eligibleCards.Add(possibleCard);
                }

                //Discard the card with the highest value out of the possible ones
                if (eligibleCards.Count > 0)
                {
                    Card bestCard = eligibleCards.OrderByDescending(c => c.Value).First();
                    newPossibleDiscards.Add(bestCard);
                    NewThought.Invoke("Discard " + bestCard + " without destroying the highest valued combo");
                }
                else //No card can be removed without destroying the currently best card combo
                {
                    //Find out which discarded cards allow for the highest combo of the remaining cards
                    eligibleCards = GetCardsWhichAllowHighestComboWhenRemoved(HandCardSpot.Cards);

                    if (eligibleCards.Any())
                    {
                        //Discard the card with the highest value out of the possible ones
                        var bestCard = eligibleCards.OrderByDescending(c => c.Value).First();
                        newPossibleDiscards.Add(bestCard);
                        NewThought.Invoke("Discard " + bestCard);
                    }
                    else //No card was found. Allow discarding the cards of the set/run with the lowest value
                    {
                        var minValRun = runs.OrderBy(run => run.Value).FirstOrDefault();
                        var minValSet = sets.OrderBy(set => set.Value).FirstOrDefault();
                        if (minValRun != null && minValSet != null)
                        {
                            if (minValRun.Value < minValSet.Value)
                            {
                                newPossibleDiscards.AddRange(minValRun.Cards);
                                NewThought.Invoke("Allow discarding all cards of " + minValSet);
                            }
                            else
                            {
                                newPossibleDiscards.AddRange(minValSet.Cards);
                                NewThought.Invoke("Allow discarding all cards of " + minValSet);
                            }
                        }
                        else if (minValRun != null)
                        {
                            newPossibleDiscards.AddRange(minValRun.Cards);
                            NewThought.Invoke("Allow discarding all cards of " + minValRun);
                        }
                        else if (minValSet != null)
                        {
                            newPossibleDiscards.AddRange(minValSet.Cards);
                            NewThought.Invoke("Allow discarding all cards of " + minValSet);
                        }
                        else
                            Tb.I.GameMaster.LogMsg("Could not discard a run/set with the lowest value because there are none!", LogType.Error);
                    }
                }
            }

            //If we have the freedom to choose, keep cards which can serve as singles later
            if (newPossibleDiscards.Count > 1)
            {
                var singleCards = new List<Card>();
                foreach (var entry in singleLayDownCards)
                    singleCards.Add(entry.Card);

                newPossibleDiscards = newPossibleDiscards.Except(singleCards).ToList();

                //If saving all single cards is not possible, discard the one with the highest value
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
                    NewThought.Invoke("Keeping singles " + msg.TrimEnd().TrimEnd(','));
                }
            }
            return newPossibleDiscards;
        }

        private void DiscardCardMoveFinished(Card card)
        {
            card.MoveFinished.RemoveAllListeners();
            Tb.I.DiscardStack.AddCard(card);

            //Refresh the list of possible card combos and singles for the UI
            var possibleCombos = CardUtil.GetAllPossibleCombos(HandCardSpot.Cards, Tb.I.GameMaster.GetAllCardSpotCards(), false, false);
            PossibleCardCombosChanged.Invoke(possibleCombos);
            UpdateSingleLaydownCards();

            TurnFinished.Invoke();
            playerState = PlayerState.IDLE;
        }

        private CardSpot GetEmptyCardSpot()
        {
            foreach (CardSpot spot in GetPlayerCardSpots())
            {
                if (!spot.HasCards)
                    return spot;
            }
            throw new MissingReferenceException(gameObject.name + " couldn't find an empty CardSpot");
        }
    }

}