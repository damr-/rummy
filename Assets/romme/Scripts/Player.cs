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
        public int PlayerNumber;

        public bool ShowCards = false;
        public float waitDuration = 2f;
        private float waitStartTime;
        public ScrollView OutputView;

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
        public PlayerState playerState = PlayerState.IDLE;

        public int PlayerCardCount { get { return HandCardSpot.GetCards().Count; } }

        public CardSpot HandCardSpot;
        public Transform PlayerCardSpotsParent;
        public List<CardSpot> GetPlayerCardSpots()
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
        private int currentCardPackIdx = 0, currentCardIdx = 0;

        private bool isCardBeingLaidDown;

        /// <summary>
        ///  Whether the player is currently laying runs or not, in which case it is currently laying sets
        /// </summary>
        private bool isLayingRuns = false;

        /// <summary>
        /// Whether the player already laid down cards in a preceding turn and is now able to pick up cards from the discard stack
        /// </summary>
        private bool hasLaidDown;
        #endregion

        public IObservable<Player> TurnFinished { get { return turnFinishedSubject; } }
        private readonly ISubject<Player> turnFinishedSubject = new Subject<Player>();

        public void AddCard(Card card)
        {
            HandCardSpot.AddCard(card);
            if (ShowCards)
                card.SetVisible(true);
        }

        private void Start()
        {
            if (HandCardSpot == null || PlayerCardSpotsParent == null || OutputView == null)
                throw new MissingReferenceException("Missing references on " + gameObject.name);
        }

        private void Update()
        {
            if (playerState == PlayerState.WAITING && Time.time - waitStartTime > waitDuration)
            {
                playerState = PlayerState.PLAYING;
                Play();
            }

            if (playerState == PlayerState.LAYING)
            {
                if (isCardBeingLaidDown)
                    return;

                if (currentCardSpot == null)
                {
                    currentCardSpot = GetEmptyCardSpot();
                    //Set the CardSpot type depending on the type of cards being laid down
                    currentCardSpot.Type = isLayingRuns ? CardSpot.SpotType.RUN : CardSpot.SpotType.SET;
                }

                Card card;
                if (isLayingRuns)
                    card = layDownCards.Runs[currentCardPackIdx].Cards[currentCardIdx];
                else
                    card = layDownCards.Sets[currentCardPackIdx].Cards[currentCardIdx];

                HandCardSpot.RemoveCard(card);
                // Debug.Log("Laying down " + card);
                card.MoveFinished.Subscribe(CardLayDownMoveFinished);
                card.MoveCard(currentCardSpot.transform.position, Tb.I.GameMaster.AnimateCardMovement);
                isCardBeingLaidDown = true;
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
            //TODO: check if we want to draw from discard stack
            if (false && !isServingCard && GetLaidCardsSum() >= Tb.I.GameMaster.MinimumLaySum)
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

        private void GetPossibleRunCombinations(List<LaydownCards> fixedCardsList, List<Run> runs)
        {
            for (int i = 0; i < runs.Count; i++)
            {
                var fixedRun = runs[i];

                var oldEntry = new LaydownCards(fixedCardsList.Last()); //Store the list of runs until now
                oldEntry.RemoveLastRun(); //remove the last one in case we need to undo the last added run

                fixedCardsList.Last().AddRun(fixedRun);

                //Find out which runs would be possible if we laid down the fixedRun
                //List<Run> possibleRuns = runs.Where(run => !run.Intersects(fixedRun)).ToList();

                if(runs.Count > 1 && i < runs.Count - 1)
                {
                    //All the other runs with higher index and wich do not intersect with the fixedRun are to be checked
                    List<Run> otherRuns = runs.GetRange(i+1, runs.Count - i - 1).Where(run => !run.Intersects(fixedRun)).ToList();

                    if(otherRuns.Count > 0)
                    {
                        GetPossibleRunCombinations(fixedCardsList, otherRuns);
                    }
                    else //There are no possible runs that can be laid down with the fixed one
                    {
                        fixedCardsList.Add(new LaydownCards(oldEntry));
                    }
                }
                else
                {
                    fixedCardsList.Add(new LaydownCards(oldEntry)); //Add the entry for the next possible permutation
                }
            }
        }

        private void GetPossibleCardCombinations(List<LaydownCards> combinations, List<Set> sets, List<Run> runs)
        {
            //Iterate through all possible combinations of sets
            for (int i = 0; i < sets.Count; i++)
            {
                //Assume we lay down the current set
                var fixedSet = sets[i];
                
                var previousCombination = new LaydownCards(combinations.Last()); //Store the list of sets until now
                previousCombination.RemoveLastSet(); //remove the last one in case we need to undo the last added set
                
                combinations.Last().AddSet(fixedSet); //The fixed list of set has to include the new one

                //What runs would be possible then?
                List<Run> possibleRuns = runs.Where(run => !run.Intersects(fixedSet)).ToList();

                if (sets.Count > 1 && i < sets.Count - 1)   //All the other sets with higher index are to be checked 
                {
                    List<Set> otherSets = sets.GetRange(i+1, sets.Count - i - 1);
                    GetPossibleCardCombinations(combinations, otherSets, possibleRuns);
                }
                else
                {
                    //All possible sets are through, continue with runs
                    GetPossibleRunCombinations(combinations, possibleRuns);
                    //fixedCardsList.Add(new LaydownCards(oldEntry)); //This permutation is done, add new entry for the next one
                }
            }

            //Check the possible runs when no sets are fixed
            combinations.Add(new LaydownCards());
            GetPossibleRunCombinations(combinations, runs);
        }

        private void Play()
        {
            //TODO: check if single cards fit with already lying cards at card spots

            //If it's the first round, just discard a card and finish turn
            if (Tb.I.GameMaster.RoundCount == 1)
            {
                DiscardUnusableCard();
                return;
            }

            sets = new List<Set>(HandCardSpot.GetCards().GetSets());
            runs = new List<Run>(HandCardSpot.GetCards().GetRuns());

            var combinations = new List<LaydownCards>(){new LaydownCards()};            
            GetPossibleCardCombinations(combinations, sets, runs);

            OutputView.ClearMessages();
            OutputView.PrintMessage(new ScrollView.Message("Possibilities:"));

            LaydownCards bestValueCombination = new LaydownCards();
            int curVal = 0, highestVal = 0;

            //Output all possibilities and calculate the most valuable one
            foreach(LaydownCards possibility in combinations)
            {
                if(possibility.Count == 0)
                    continue;
                    
                curVal = possibility.Value;
                if (curVal > highestVal)
                {
                    highestVal = curVal;
                    bestValueCombination = new LaydownCards(possibility);
                }

                string msg = "";
                if(possibility.Sets.Count > 0)
                {
                    foreach(Set set in possibility.Sets)
                        msg += set + ", ";
                }
                if(possibility.Runs.Count > 0)
                {
                    foreach(Run run in possibility.Runs)
                        msg += run + ", ";
                    msg = msg.TrimEnd().TrimEnd(',');
                }
                msg += " (" + curVal + ")";
                OutputView.PrintMessage(new ScrollView.Message(msg));
            }

            #region Todo-Joker
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

            layDownCards = new LaydownCards(bestValueCombination);

            if (layDownCards.Count == 0)
            {
                DiscardUnusableCard();
                return;
            }

            int sum = 0;
            if (!hasLaidDown)
            {
                foreach (var set in layDownCards.Sets)
                    sum += set.Value;
                foreach (var run in layDownCards.Runs)
                    sum += run.Value;
            }

            if (hasLaidDown || sum >= Tb.I.GameMaster.MinimumLaySum)
            {
                foreach(var set in layDownCards.Sets)
                    Debug.Log("Laying down set: " + set);
                foreach(var run in layDownCards.Runs)
                    Debug.Log("Laying down run: " + run);
                currentCardPackIdx = 0;
                currentCardIdx = 0;

                //Start laying runs if there are no sets to lay
                isLayingRuns = layDownCards.Sets.Count == 0;

                if (!hasLaidDown)
                    hasLaidDown = true;

                isCardBeingLaidDown = false;
                playerState = PlayerState.LAYING;
            }
            else
            {
                DiscardUnusableCard();
            }
        }

        private void CardLayDownMoveFinished(Card card)
        {
            isCardBeingLaidDown = false;
            currentCardSpot.AddCard(card);

            int cardCount, cardPackCount;

            if (isLayingRuns)
            {
                cardCount = layDownCards.Runs[currentCardPackIdx].Count;
                cardPackCount = layDownCards.Runs.Count;
            }
            else
            {
                cardCount = layDownCards.Sets[currentCardPackIdx].Count;
                cardPackCount = layDownCards.Sets.Count;
            }

            if (currentCardIdx == cardCount - 1)
            {
                currentCardIdx = 0;
                currentCardPackIdx++;
                currentCardSpot = null;     //Find a new spot for the next pack of cards

                if (currentCardPackIdx == cardPackCount)
                {
                    // End turn if all sets and runs are laid down
                    // or the sets are finished and there are no runs to lay
                    if (isLayingRuns || layDownCards.Runs.Count == 0)
                        DiscardUnusableCard();
                    else// Start laying runs otherwise
                    {   
                        currentCardPackIdx = 0;
                        isLayingRuns = true;
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

            //Randomly choose a card to discard
            Card card = possibleDiscards[UnityEngine.Random.Range(0, possibleDiscards.Count - 1)];

            //In case any discardable card is not a Joker, make sure the discarded one is NOT a Joker
            if(possibleDiscards.Any(c => c.Rank != Card.CardRank.JOKER))
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

        public CardSpot GetEmptyCardSpot()
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