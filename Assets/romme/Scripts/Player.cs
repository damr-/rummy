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

        #region CardSets
        private List<Set> sets = new List<Set>();
        private List<Run> runs = new List<Run>();
        // private List<KeyValuePair<Card.CardRank, List<Card>>> possibleJokerSets = new List<KeyValuePair<Card.CardRank, List<Card>>>();
        private LaydownCards layDownCards = new LaydownCards();
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

        public IObservable<List<LaydownCards>> PossibleCardCombosChanged { get { return possibleCardCombos; } }
        private readonly ISubject<List<LaydownCards>> possibleCardCombos = new Subject<List<LaydownCards>>();

        private void AddCard(Card card)
        {
            HandCardSpot.AddCard(card);
            if (ShowCards)
                card.SetVisible(true);
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
                Play();
            }

            if (playerState == PlayerState.LAYING)
            {
                if (isCardBeingLaidDown)
                    return;

                isCardBeingLaidDown = true;

                if (currentCardSpot == null && layStage != LayStage.SINGLES)
                {
                    currentCardSpot = GetEmptyCardSpot();
                    currentCardSpot.Type = (layStage == LayStage.RUNS) ? CardSpot.SpotType.RUN : CardSpot.SpotType.SET;
                }
                else if (layStage == LayStage.SINGLES) //For singles, the corresponding card spot is stored in 'singleLayDownCards'
                {
                    currentCardSpot = singleLayDownCards[currentCardIdx].Value;
                }

                Card card;
                switch (layStage)
                {
                    case LayStage.SETS:
                        card = layDownCards.Sets[currentCardPackIdx].Cards[currentCardIdx];
                        break;
                    case LayStage.RUNS:
                        card = layDownCards.Runs[currentCardPackIdx].Cards[currentCardIdx];
                        break;
                    default: //LayStage.SINGLES
                        card = singleLayDownCards[currentCardIdx].Key;
                        break;
                }

                HandCardSpot.RemoveCard(card);
                // Debug.Log("Laying down " + card);
                card.MoveFinished.Subscribe(CardLayDownMoveFinished);
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
                //Check if we want to draw from discard stack
                //          Note that every player always makes sure not to discard a
                //          card which can be added to an existing, laid-down card pack.
                //          Therefore, no need to check for that case here.
                Card discardedCard = Tb.I.DiscardStack.PeekCard();
                var hypotheticalHandCards = new List<Card>(HandCardSpot.GetCards());
                hypotheticalHandCards.Add(discardedCard);
                var sets = CardUtil.GetPossibleSets(hypotheticalHandCards);
                var runs = CardUtil.GetPossibleRuns(hypotheticalHandCards);
                var bestCardCombo = GetBestCardCombo(sets, runs);
                int hypotheticalValue = bestCardCombo.Value;

                sets = new List<Set>(CardUtil.GetPossibleSets(HandCardSpot.GetCards()));
                runs = new List<Run>(CardUtil.GetPossibleRuns(HandCardSpot.GetCards()));
                bestCardCombo = GetBestCardCombo(sets, runs);
                int currentValue = bestCardCombo.Value;

                if (hypotheticalValue > currentValue)
                    takeFromDiscardStack = true;
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
                playerState = PlayerState.IDLE;
            else
            {
                playerState = PlayerState.WAITING;
                waitStartTime = Time.time;
            }
        }

        private LaydownCards GetBestCardCombo(List<Set> sets, List<Run> runs, bool broadCastPossibleCombos = false)
        {
            var combinations = new List<LaydownCards>() { new LaydownCards() };
            CardUtil.GetPossibleCombinations(combinations, sets, runs);

            if (combinations.Last().Count > 0)
                combinations.Add(new LaydownCards());
            //Check the possible runs when no sets are fixed
            CardUtil.GetPossibleRunCombinations(combinations, runs);

            combinations = combinations.Where(ldc => ldc.Count > 0).ToList();
            
            if(broadCastPossibleCombos)
                possibleCardCombos.OnNext(new List<LaydownCards>(combinations));

            return CardUtil.GetHighestValueCombination(combinations);
        }

        private void Play()
        {
            //If it's the first round, just discard a card and finish turn
            if (Tb.I.GameMaster.RoundCount == 1)
            {
                DiscardUnusableCard();
                return;
            }

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

            sets = new List<Set>(CardUtil.GetPossibleSets(HandCardSpot.GetCards()));
            runs = new List<Run>(CardUtil.GetPossibleRuns(HandCardSpot.GetCards()));
            layDownCards = GetBestCardCombo(sets, runs, true);

            //If the player has not laid down card packs yet, check if their sum would be enough to do so
            if (!hasLaidDown)
                hasLaidDown = layDownCards.Value >= Tb.I.GameMaster.MinimumLaySum;

            singleLayDownCards = new List<KeyValuePair<Card, CardSpot>>();
            if (hasLaidDown)
            {
                //While there can be cards laid down to existing spots, check an additional time
                bool canFitCard = false;
                var reservedSpots = new List<KeyValuePair<CardSpot, List<Card>>>();

                //Check all cards which will not be laid down anyway as part of a set or a run
                List<Card> availableCards = new List<Card>(HandCardSpot.GetCards());
                foreach (var set in layDownCards.Sets)
                    availableCards = availableCards.Except(set.Cards).ToList();
                foreach (var run in layDownCards.Runs)
                    availableCards = availableCards.Except(run.Cards).ToList();

                do
                {
                    //Find single cards which fit with already lying cards
                    var tmpCardSpots = new List<CardSpot>(GetPlayerCardSpots());
                    var otherPlayerCardSpots = Tb.I.GameMaster.GetOtherPlayer(this).GetPlayerCardSpots();
                    tmpCardSpots.AddRange(otherPlayerCardSpots);

                    canFitCard = false;

                    foreach (CardSpot cardSpot in tmpCardSpots)
                    {
                        foreach (Card card in availableCards)
                        {
                            //If the card is already used elsewhere, skip
                            if (singleLayDownCards.Any(kvp => kvp.Key == card))
                                continue;

                            if (!cardSpot.CanFit(card))
                                continue;

                            //Find all entries which will fill the current cardspot
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
            }

            //If there's nothing to lay down or the points are not enough discard a card and end the turn
            if ((layDownCards.Count == 0 && singleLayDownCards.Count == 0) || !hasLaidDown)
            {
                DiscardUnusableCard();
                return;
            }

            //Lay down all possible cards
            foreach (var set in layDownCards.Sets)
                Debug.Log("Laying down set: " + set);
            foreach (var run in layDownCards.Runs)
                Debug.Log("Laying down run: " + run);

            if (singleLayDownCards.Count > 0)
            {
                string singlesOutput = "";
                singleLayDownCards.ForEach(c => singlesOutput += "[" + c.Key + ":" + c.Value.gameObject.name + "], ");
                Debug.Log("Laying down singles: " + singlesOutput.TrimEnd().TrimEnd(','));
            }

            //FIXME: Don'T allow laying down all cards (laydownCards.Count must be != PlayerHandSpot.Count)

            //FIXME: INVESTIGATE Discard stack card disappears (?) NO_JOKER, SEED 12231, Round 14-ish or higher

            currentCardPackIdx = 0;
            currentCardIdx = 0;

            layStage = LayStage.SETS;
            if (layDownCards.Sets.Count == 0)
            {
                layStage = LayStage.RUNS;
                if (layDownCards.Runs.Count == 0)
                    layStage = LayStage.SINGLES;
            }

            isCardBeingLaidDown = false;
            playerState = PlayerState.LAYING;
        }

        private void CardLayDownMoveFinished(Card card)
        {
            isCardBeingLaidDown = false;
            currentCardSpot.AddCard(card);

            int cardCount, cardPackCount;
            switch (layStage)
            {
                case LayStage.SETS:
                    cardCount = layDownCards.Sets[currentCardPackIdx].Count;
                    cardPackCount = layDownCards.Sets.Count;
                    break;
                case LayStage.RUNS:
                    cardCount = layDownCards.Runs[currentCardPackIdx].Count;
                    cardPackCount = layDownCards.Runs.Count;
                    break;
                default: //LayStage.SINGLES
                    cardCount = singleLayDownCards.Count;
                    cardPackCount = 1;
                    break;
            }

            if (currentCardIdx == cardCount - 1)
            {
                currentCardIdx = 0;
                currentCardPackIdx++;
                currentCardSpot = null;     //Find a new spot for the next set or run of cards

                if (currentCardPackIdx == cardPackCount)
                {
                    // End turn if all singles have been laid down
                    // or if the runs are finished and there are no singles to lay down
                    // or if the sets are finished and there are no runs or singles to lay down
                    if (layStage == LayStage.SINGLES ||
                        (layStage == LayStage.RUNS && singleLayDownCards.Count == 0) ||
                        (layStage == LayStage.SETS && layDownCards.Runs.Count == 0 && singleLayDownCards.Count == 0))
                    {
                        DiscardUnusableCard();
                    }
                    else// Start laying the next cards otherwise
                    {
                        currentCardPackIdx = 0;
                        layStage = (layStage == LayStage.SETS) ? LayStage.RUNS : LayStage.SINGLES;
                    }
                }
            }
            else
                currentCardIdx++;
        }

        private void DiscardUnusableCard()
        {
            playerState = PlayerState.DISCARDING;

            //Get all player cards except the ones which will be laid down
            List<Card> possibleDiscards = new List<Card>(HandCardSpot.GetCards());
            foreach (var run in runs)
                possibleDiscards = possibleDiscards.Except(run.Cards).ToList();
            foreach (var set in sets)
                possibleDiscards = possibleDiscards.Except(set.Cards).ToList();

            //TODO: exclude single cards which will be laid to an existing card spot's card pack
            //Imagine that a player might not have laid cards yet but still want to keep a card 
            // which they want to add to their opponent's card stack once they have laid cards down

            //Randomly choose a card to discard
            Card card = possibleDiscards[UnityEngine.Random.Range(0, possibleDiscards.Count - 1)];

            //In case any discardable card is not a Joker, make sure the discarded one is NOT a Joker
            if (possibleDiscards.Any(c => c.Rank != Card.CardRank.JOKER))
            {
                while (card.Rank == Card.CardRank.JOKER)
                    card = possibleDiscards[UnityEngine.Random.Range(0, possibleDiscards.Count - 1)];
            }

            HandCardSpot.RemoveCard(card);
            card.MoveFinished.Subscribe(DiscardCardMoveFinished);
            card.MoveCard(Tb.I.DiscardStack.GetNextCardPos(), Tb.I.GameMaster.AnimateCardMovement);
        }

        private void DiscardCardMoveFinished(Card card)
        {
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
            Debug.LogWarning(gameObject.name + " couldn't find an empty CardSpot");
            return null;
        }

        public int GetLaidCardsSum()
        {
            int sum = 0;
            GetPlayerCardSpots().ForEach(spot => sum += spot.GetValue());
            return sum;
        }

    }
}