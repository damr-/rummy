using System.Collections.Generic;
using UniRx;
using System;
using System.Linq;
using UnityEngine;
using romme.Cards;
using romme.Utility;
using romme.UI;

namespace romme
{

    public class Player : MonoBehaviour
    {
        public bool ShowCards = false;
        private float waitStartTime;

        private IDisposable cardMoveSubscription = Disposable.Empty;

        public enum PlayerState
        {
            IDLE = 0,
            DRAWING = 1,
            WAITING = 2,
            PLAYING = 3,
            LAYING = 4,
            DISCARDING = 5
        }
        public PlayerState playerState { get; private set; } = PlayerState.IDLE;

        private CardSpot HandCardSpot;
        public int PlayerCardCount { get { return HandCardSpot.GetCards().Count; } }
        public int PlayerHandValue { get { return HandCardSpot.GetValue(); } }

        public Transform PlayerCardSpotsParent;
        private List<CardSpot> GetPlayerCardSpots()
        {
            if (playerCardSpots.Count == 0)
                playerCardSpots = PlayerCardSpotsParent.GetComponentsInChildren<CardSpot>().ToList();
            return playerCardSpots;
        }
        private List<CardSpot> playerCardSpots = new List<CardSpot>();
        private CardSpot currentCardSpot;
        public int GetLaidCardsSum() => GetPlayerCardSpots().Sum(spot => spot.GetValue());

        #region CardSets
        private List<Set> possibleSets = new List<Set>();
        private List<Run> possibleRuns = new List<Run>();
        // private List<KeyValuePair<Card.CardRank, List<Card>>> possibleJokerSets = new List<KeyValuePair<Card.CardRank, List<Card>>>();
        private CardCombo laydownCards = new CardCombo();
        private List<KeyValuePair<Card, CardSpot>> singleLayDownCards = new List<KeyValuePair<Card, CardSpot>>();
        private int currentCardPackIdx = 0, currentCardIdx = 0;

        private bool isCardBeingLaidDown;

        private enum LayStage
        {
            SETS = 0,
            RUNS = 1,
            SINGLES = 2
        }

        /// <summary>
        ///  Whether the player is currently laying runs or not, in which case it is currently laying sets
        /// </summary>
        private LayStage layStage = LayStage.SETS;

        /// <summary>
        /// Whether the player can lay down cards and pick up cards from the discard stack
        /// </summary>
        private bool hasLaidDown;
        #endregion

        public IObservable<Player> TurnFinished { get { return turnFinishedSubject; } }
        private readonly ISubject<Player> turnFinishedSubject = new Subject<Player>();

        public IObservable<List<CardCombo>> PossibleCardCombosChanged { get { return possibleCardCombos; } }
        private readonly ISubject<List<CardCombo>> possibleCardCombos = new Subject<List<CardCombo>>();

        public IObservable<List<KeyValuePair<Card, CardSpot>>> PossibleSinglesChanged { get { return possibleSingles; } }
        private readonly ISubject<List<KeyValuePair<Card, CardSpot>>> possibleSingles = new Subject<List<KeyValuePair<Card, CardSpot>>>();

        private void AddCard(Card card)
        {
            HandCardSpot.AddCard(card);
            if (ShowCards)
                card.SetVisible(true);
        }

        public void ResetPlayer()
        {
            hasLaidDown = false;
            playerState = PlayerState.IDLE;
            GetPlayerCardSpots().ForEach(spot => spot.ResetSpot());
            HandCardSpot.ResetSpot();
            possibleCardCombos.OnNext(new List<CardCombo>());
            possibleSingles.OnNext(new List<KeyValuePair<Card, CardSpot>>());
        }

        private void Start()
        {
            if (PlayerCardSpotsParent == null)
                throw new MissingReferenceException("Missing 'PlayerCardSpotsParent' reference for " + gameObject.name);
            HandCardSpot = GetComponentInChildren<CardSpot>();
        }

        private void Update()
        {
            if (playerState == PlayerState.WAITING && Time.time - waitStartTime > Tb.I.GameMaster.PlayerWaitDuration)
            {
                playerState = PlayerState.PLAYING;

                if (Tb.I.GameMaster.RoundCount < Tb.I.GameMaster.EarliestAllowedLaydownRound || !hasLaidDown)
                {
                    DiscardUnusableCard();
                }
                else
                {
                    // laydownCards.Sets.ForEach(set => Debug.Log("Laying down set: " + set));
                    // laydownCards.Runs.ForEach(run => Debug.Log("Laying down run: " + run));

                    playerState = PlayerState.LAYING;
                    isCardBeingLaidDown = false;
                    currentCardPackIdx = 0;
                    currentCardIdx = 0;
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
                    currentCardSpot = singleLayDownCards[currentCardIdx].Value;
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
                        card = singleLayDownCards[currentCardIdx].Key;
                        break;
                }

                HandCardSpot.RemoveCard(card);
                cardMoveSubscription = card.MoveFinished.Subscribe(CardLayDownMoveFinished);
                card.MoveCard(currentCardSpot.transform.position, Tb.I.GameMaster.AnimateCardMovement);
            }
        }

        public void BeginTurn()
        {
            DrawCard(false);
        }

        public void DrawCard(bool isServingCard)
        {
            playerState = PlayerState.DRAWING;

            Card card;
            bool takeFromDiscardStack = false;
            if (!isServingCard && hasLaidDown)
            {
                // Check if we want to draw from discard stack
                //          Note that every player always makes sure not to discard a
                //          card which can be added to an already laid-down card pack.
                //          Therefore, no need to check for that case here.
                Card discardedCard = Tb.I.DiscardStack.PeekCard();
                var hypotheticalHandCards = new List<Card>(HandCardSpot.GetCards());
                hypotheticalHandCards.Add(discardedCard);
                var sets = CardUtil.GetPossibleSets(hypotheticalHandCards);
                var runs = CardUtil.GetPossibleRuns(hypotheticalHandCards);
                var hypotheticalBestCombo = GetBestCardCombo(sets, runs, false);
                int hypotheticalValue = hypotheticalBestCombo.Value;

                sets = new List<Set>(CardUtil.GetPossibleSets(HandCardSpot.GetCards()));
                runs = new List<Run>(CardUtil.GetPossibleRuns(HandCardSpot.GetCards()));
                int currentValue = GetBestCardCombo(sets, runs, false).Value;

                if (hypotheticalValue > currentValue)
                {
                    Debug.Log(gameObject.name + " takes " + discardedCard + " from discard pile to finish " + hypotheticalBestCombo);
                    takeFromDiscardStack = true;
                }
            }

            if (takeFromDiscardStack)
                card = Tb.I.DiscardStack.DrawCard();
            else
                card = Tb.I.CardStack.DrawCard();

            cardMoveSubscription = card.MoveFinished.Subscribe(c => DrawCardFinished(c, isServingCard));
            card.MoveCard(transform.position, Tb.I.GameMaster.AnimateCardMovement);
        }

        private void DrawCardFinished(Card card, bool isServingCard)
        {
            cardMoveSubscription.Dispose();
            AddCard(card);
            if (isServingCard)
            {
                playerState = PlayerState.IDLE;
                return;
            }

            //As soon as the card was drawn, figure out the possible cards combos (BEFORE waiting)
            #region Joker
            //TODO: Get possible runs with joker cards and choose which RUN or SET to play
            /*
            var possibleJokerSetsColoured = PlayerCards.GetJokerSets(possibleSets);
            // Choose which SETS to play with joker (the ones with the highest point sum will be chosen)
            if(possibleJokerSetsColoured.Count > 0)
            {
                possibleJokerSets = new List<KeyValuePair<Card.CardRank, List<Card>>>();
                List<Card> usedJokerCards = new List<Card>();
                int jokerCount = PlayerCards.Where(c => c.Rank == Card.CardRank.JOKER).Count();

                do
                {
                    var possibleSet = possibleJokerSetsColoured[0];
                    possibleJokerSetsColoured.RemoveAt(0);

                    var jokerCard = PlayerCards.FirstOrDefault(c => c.Rank == Card.CardRank.JOKER &&
                                                                !usedJokerCards.Contains(c) &&
                                                                c.Color == possibleSet.Key);
                    if (jokerCard == null)
                        continue;

                    usedJokerCards.Add(jokerCard);
                    possibleSet.Value.Value.Add(jokerCard);
                    possibleJokerSets.Add(possibleSet.Value);
                } while (possibleJokerSetsColoured.Count > 0 && usedJokerCards.Count < jokerCount);

            }*/
            #endregion

            possibleSets = new List<Set>(CardUtil.GetPossibleSets(HandCardSpot.GetCards()));
            possibleRuns = new List<Run>(CardUtil.GetPossibleRuns(HandCardSpot.GetCards()));
            laydownCards = GetBestCardCombo(possibleSets, possibleRuns, true);
            UpdateSingleLaydownCards();

            if (Tb.I.GameMaster.RoundCount >= Tb.I.GameMaster.EarliestAllowedLaydownRound)
            {
                //If the player has not laid down card packs yet, check if their sum would be enough to do so
                if (!hasLaidDown)
                    hasLaidDown = laydownCards.Value >= Tb.I.GameMaster.MinimumLaySum;

                //At least one card must remain when laying down
                if (hasLaidDown && laydownCards.CardCount == HandCardSpot.GetCards().Count)
                    KeepOneCard();
            }

            playerState = PlayerState.WAITING;
            waitStartTime = Time.time;
        }

        private CardCombo GetBestCardCombo(List<Set> sets, List<Run> runs, bool broadCastPossibleCombos)
        {
            var possibleCombos = CardUtil.GetAllPossibleCombos(sets, runs, HandCardSpot.GetCards().Count);

            if (broadCastPossibleCombos)
                possibleCardCombos.OnNext(possibleCombos);

            possibleCombos = possibleCombos.OrderByDescending(c => c.Value).ToList();
            
            return possibleCombos.Count == 0 ? new CardCombo() : possibleCombos[0];
        }

        private void UpdateSingleLaydownCards()
        {
            singleLayDownCards = new List<KeyValuePair<Card, CardSpot>>();
            bool canFitCard = false;
            var reservedSpots = new List<KeyValuePair<CardSpot, List<Card>>>();

            //Check all cards which will not be laid down anyway as part of a set or a run
            List<Card> availableCards = new List<Card>(HandCardSpot.GetCards());
            foreach (var set in laydownCards.Sets)
                availableCards = availableCards.Except(set.Cards).ToList();
            foreach (var run in laydownCards.Runs)
                availableCards = availableCards.Except(run.Cards).ToList();

            //Find single cards which fit with already lying cards
            do
            {
                var cardSpots = new List<CardSpot>(GetPlayerCardSpots());
                var otherPlayerCardSpots = Tb.I.GameMaster.GetOtherPlayer(this).GetPlayerCardSpots();
                cardSpots.AddRange(otherPlayerCardSpots);

                canFitCard = false;

                foreach (CardSpot cardSpot in cardSpots)
                {
                    foreach (Card card in availableCards)
                    {
                        //If the card is already used elsewhere, skip
                        if (singleLayDownCards.Any(kvp => kvp.Key == card))
                            continue;

                        if (!cardSpot.CanFit(card))
                            continue;

                        //Find all single cards which will be added to the current cardspot
                        var plannedMoves = singleLayDownCards.Where(kvp => kvp.Value == cardSpot).ToList();
                        bool alreadyPlanned = false;
                        foreach (var entry in plannedMoves)
                        {
                            //If a card with the same rank and suit is already planned to be added to cardspot
                            //the current card cannot be added anymore
                            if (entry.Key.Suit == card.Suit && entry.Key.Rank == card.Rank)
                            {
                                alreadyPlanned = true;
                                break;
                            }
                        }
                        if (alreadyPlanned)
                            continue;

                        var newEntry = new KeyValuePair<Card, CardSpot>(card, cardSpot);
                        singleLayDownCards.Add(newEntry);
                        canFitCard = true;
                    }
                }
            } while (canFitCard);
            possibleSingles.OnNext(singleLayDownCards);
        }

        /// <summary>
        /// Figures out a way to keep at least one card in the player's hand.
        /// This is needed in the case the player wanted to lay down every single card they had left in their hand.
        /// First, we try to keep one of the singleLayDownCards, or one from quad-sets or runs with more than 3 cards
        /// If no card is found, a whole set or run is not laid down
        /// </summary>
        private void KeepOneCard()
        {
            bool removedSingleCard = false;
            Debug.Log("Need to keep one card");
            if (singleLayDownCards.Count > 0)
            {
                Debug.Log("Removed single laydown card " + singleLayDownCards[singleLayDownCards.Count - 1]);
                singleLayDownCards.RemoveAt(singleLayDownCards.Count - 1);
                removedSingleCard = true;
            }
            else
            {
                //Keep one card of a set or run with more than 3 cards
                var quadSet = laydownCards.Sets.FirstOrDefault(s => s.Count == 4);
                if (quadSet != null)
                {
                    Debug.Log("Removed the last card of set " + quadSet);
                    Card lastCard = quadSet.RemoveLastCard();

                    //Remove all sets, which include the card, from the list of possible sets 
                    possibleSets = possibleSets.Where(set => !set.Cards.Contains(lastCard)).ToList();

                    removedSingleCard = true;
                }
                else
                {
                    var quadRun = laydownCards.Runs.FirstOrDefault(r => r.Count > 3);
                    if (quadRun != null)
                    {
                        Debug.Log("Removed the last card of run " + quadRun);
                        Card lastCard = quadRun.RemoveLastCard();
                        //Remove all runs, which include the card, from the list of possible runs
                        possibleRuns = possibleRuns.Where(run => !run.Cards.Contains(lastCard)).ToList();
                        removedSingleCard = true;
                    }
                    else
                    {
                        Debug.LogWarning("Could not find a single card to remove");
                    }
                }
            }

            if (removedSingleCard) //One card will be kept on hand, we're done
                return;

            //No card was removed yet, we have to keep a whole pack on hand            
            if (laydownCards.Sets.Count > 0)
            {
                var lastSet = laydownCards.Sets.Last();
                Debug.Log("Removing the whole set " + lastSet);
                Set set = laydownCards.RemoveLastSet();
                //Remove any set which shares cards with the removed one
                possibleSets = possibleSets.Where(s => !s.Intersects(set)).ToList();
            }
            else if (laydownCards.Runs.Count > 0)
            {
                var lastRun = laydownCards.Runs.Last();
                Debug.Log("No set found to remove. Removing the whole run " + lastRun);
                Run run = laydownCards.RemoveLastRun();
                //Remove any run which shares cards with the removed one
                possibleRuns = possibleRuns.Where(r => !r.Intersects(run)).ToList();
            }
            else
            {
                Debug.LogError("No single cards and no sets or runs found to discard. This should never happen!");
            }
        }

        private void CardLayDownMoveFinished(Card card)
        {
            cardMoveSubscription.Dispose();
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
                    break;
            }

            if (currentCardIdx < cardCount - 1)
            {
                currentCardIdx++;
                return;
            }

            //Last card of the current pack has been laid
            currentCardIdx = 0;
            currentCardPackIdx++;
            currentCardSpot = null;     //Find a new spot for the next set or run of cards

            if (currentCardPackIdx < cardPackCount)
                return;

            //Everything has been laid down
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

        private void LayingCardsDone()
        {
            //With only one card left, just end the game
            if (HandCardSpot.GetCards().Count == 1)
            {
                DiscardUnusableCard();
                return;
            }

            //Check if there are any singles to lay down
            UpdateSingleLaydownCards();

            if (singleLayDownCards.Count == HandCardSpot.GetCards().Count)
                KeepOneCard();

            if (singleLayDownCards.Count > 0)
            {
                string msg = "";
                singleLayDownCards.ForEach(c => msg += "[" + c.Key + ":" + c.Value.gameObject.name + "], ");
                // Debug.Log("Laying down singles: " + msg.TrimEnd().TrimEnd(','));

                currentCardIdx = 0;
                layStage = LayStage.SINGLES;
            }
            else
                DiscardUnusableCard();
        }

        private void DiscardUnusableCard()
        {
            playerState = PlayerState.DISCARDING;

            //Get all player cards except the ones which will be laid down as sets or runs
            List<Card> possibleDiscards = new List<Card>(HandCardSpot.GetCards());

            //Keep possible runs/sets/singles on hand for laying down later
            if (!hasLaidDown)
                possibleDiscards = KeepUsableCards(possibleDiscards);

            if (possibleDiscards.Count == 0)
            {
                Debug.LogWarning("No possible cards to discard. (This should not happen!) Choosing random one");
                possibleDiscards.Add(HandCardSpot.GetCards().ElementAt(UnityEngine.Random.Range(0, HandCardSpot.GetCards().Count)));
            }

            //Discard the card with the highest value
            Card card = possibleDiscards.OrderByDescending(c => c.Value).FirstOrDefault();

            //In case any discardable card is not a Joker, make sure the discarded one is NOT a Joker
            if (card.Rank == Card.CardRank.JOKER && possibleDiscards.Any(c => c.Rank != Card.CardRank.JOKER))
            {
                do
                {
                    card = possibleDiscards[UnityEngine.Random.Range(0, possibleDiscards.Count)];
                } while (card.Rank == Card.CardRank.JOKER);
            }

            HandCardSpot.RemoveCard(card);
            cardMoveSubscription = card.MoveFinished.Subscribe(DiscardCardMoveFinished);
            card.MoveCard(Tb.I.DiscardStack.GetNextCardPos(), Tb.I.GameMaster.AnimateCardMovement);
        }

        /// <summary>
        /// Removes all the cards from 'possibleDiscards' which are part of a set/run or 
        /// are going to be laid down to an existing pack as a single
        /// At least one card remains in 'possibleDiscards', being either a whole card pack 
        /// or the lowest valued single card
        /// </summary>
        private List<Card> KeepUsableCards(List<Card> possibleDiscards)
        {
            foreach (var run in possibleRuns)
                possibleDiscards = possibleDiscards.Except(run.Cards).ToList();
            foreach (var set in possibleSets)
                possibleDiscards = possibleDiscards.Except(set.Cards).ToList();

            // If the hand only consists of possible sets and runs, discard the card with the highest
            // value which does also not destroy the currently best card combo
            if (possibleDiscards.Count == 0)
            {
                int maxValue = GetBestCardCombo(possibleSets, possibleRuns, false).Value;
                List<Card> possibleCards = new List<Card>();
                foreach (Card possibleCard in HandCardSpot.GetCards())
                {
                    var hypotheticalHandCards = new List<Card>(HandCardSpot.GetCards());
                    hypotheticalHandCards.Remove(possibleCard);
                    var sets = CardUtil.GetPossibleSets(hypotheticalHandCards);
                    var runs = CardUtil.GetPossibleRuns(hypotheticalHandCards);
                    int hypotheticalValue = GetBestCardCombo(sets, runs, false).Value;
                    if (hypotheticalValue == maxValue)
                        possibleCards.Add(possibleCard);
                }

                //Discard the card with the highest value out of the possible ones
                if (possibleCards.Count > 0)
                {
                    Card bestCard = possibleCards.OrderByDescending(c => c.Value).ElementAt(0);
                    possibleDiscards.Add(bestCard);
                }
                else //No card can be removed without destroying the currently best card combo
                {   //Allow to discard the run or set with the lowerst value
                    var minValRun = possibleRuns.OrderBy(run => run.Value).FirstOrDefault();
                    var minValSet = possibleSets.OrderBy(set => set.Value).FirstOrDefault();
                    if (minValRun != null && minValSet != null)
                    {
                        if (minValRun.Value < minValSet.Value)
                            possibleDiscards.AddRange(minValRun.Cards);
                        else
                            possibleDiscards.AddRange(minValSet.Cards);
                    }
                    else if (minValRun != null)
                    {
                        possibleDiscards.AddRange(minValRun.Cards);
                    }
                    else if (minValSet != null)
                    {
                        possibleDiscards.AddRange(minValSet.Cards);
                    }
                    else
                    {
                        Debug.Log("Could not discard a run/set with the lowest value because there are none");
                    }
                }
            }

            //If we have the freedom to choose, keep cards which can serve as singles later
            if(possibleDiscards.Count > 1)
            {
                //Single cards which will be laid down also have to be excluded
                //This is only important for the following scenario:
                //      Imagine that a player might not have laid cards yet but still want to keep a card 
                //      which they want to add to their opponent's card stack once they have laid cards down
                UpdateSingleLaydownCards();
                var singleCards = new List<Card>();
                foreach (var entry in singleLayDownCards)
                    singleCards.Add(entry.Key);

                //In case all remaining cards are possible single cards, keep the one with the lowest value
                // if(possibleDiscards.Except(singleCards).Count() == 0)
                // {
                //     var lowestCard = singleCards.OrderBy(c => c.Value).First();
                //     singleCards.Remove(lowestCard);
                //     Debug.Log(lowestCard);
                // }
                possibleDiscards = possibleDiscards.Except(singleCards).ToList();

                //If saving all single cards is not possible, discard the one with the highest value
                if(possibleDiscards.Count == 0)
                    possibleDiscards.Add(singleCards.OrderByDescending(c => c.Value).FirstOrDefault());
            }

            return possibleDiscards;
        }

        private void DiscardCardMoveFinished(Card card)
        {
            //Refresh the list of possible card combos and singles in the UI
            var sets = new List<Set>(CardUtil.GetPossibleSets(HandCardSpot.GetCards()));
            var runs = new List<Run>(CardUtil.GetPossibleRuns(HandCardSpot.GetCards()));
            var possibleCombos = CardUtil.GetAllPossibleCombos(sets, runs, HandCardSpot.GetCards().Count);
            possibleCardCombos.OnNext(possibleCombos);
            UpdateSingleLaydownCards();

            cardMoveSubscription.Dispose();
            Tb.I.DiscardStack.AddCard(card);
            turnFinishedSubject.OnNext(this);
            playerState = PlayerState.IDLE;
        }

        private CardSpot GetEmptyCardSpot()
        {
            foreach (CardSpot spot in GetPlayerCardSpots())
            {
                if (!spot.HasCards)
                    return spot;
            }
            Debug.LogError(gameObject.name + " couldn't find an empty CardSpot");
            return null;
        }
    }

}