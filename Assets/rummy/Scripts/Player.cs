using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using rummy.Cards;
using rummy.Utility;
using UnityEngine.UI;
using TMPro;

namespace rummy
{

    public class Player : MonoBehaviour
    {
        private float waitStartTime;

        public string PlayerName { get; private set; }
        public void SetPlayerName(string name)
        {
            PlayerName = name;
            PlayerNameText.text = name;
            gameObject.name = name;
        }
        private TextMeshProUGUI PlayerNameText
        {
            get
            {
                if (_playerNameText == null)
                    _playerNameText = transform.Find("PlayerCanvas/PlayerNameHighlight/PlayerName").GetComponent<TextMeshProUGUI>();
                return _playerNameText;
            }
        }
        private TextMeshProUGUI _playerNameText;

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
        public PlayerState State { get; private set; } = PlayerState.IDLE;

        #region PlayerCardSpot
        [SerializeField]
        private bool CardsVisible = false;

        [SerializeField]
        private HandCardSpot HandCardSpot;
        public int HandCardCount => HandCardSpot.Objects.Count;
        public int GetHandCardsSum()
        {
            return HandCardSpot.Objects.Sum(c => c.Rank == Card.CardRank.JOKER ? 20 : c.Value);
        }

        [SerializeField]
        private CardSpotsNode PlayerCardSpotsNode;
        private CardSpot currentCardSpot;
        public List<CardSpot> GetPlayerCardSpots() => PlayerCardSpotsNode.Objects;
        public int GetLaidCardsSum() => PlayerCardSpotsNode.Objects.Sum(spot => spot.GetValue());

        [SerializeField]
        private TextMeshProUGUI cardCount;
        [SerializeField]
        private TextMeshProUGUI playerName;
        [SerializeField]
        private Image playerNameHighlight;
        #endregion

        #region CardSets
        /// <summary>The card sets and runs which are going to be laid down</summary>
        private CardCombo laydownCards = new();

        /// <summary>The single cards which are going to be laid down</summary>
        private List<Single> singleLayDownCards = new();

        private int currentMeldIdx = 0, currentCardIdx = 0;
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
        /// Whether the player can lay down melds and pick up cards from the discard stack
        /// </summary>
        public bool HasLaidDown { get; private set; }
        #endregion

        #region Events
        [HideInInspector]
        public UnityEvent TurnFinished = new();

        public class Event_PossibleCardCombosChanged : UnityEvent<List<CardCombo>> { }
        public Event_PossibleCardCombosChanged PossibleCardCombosChanged = new();

        public class Event_PossibleSinglesChanged : UnityEvent<List<Single>> { }
        public Event_PossibleSinglesChanged PossibleSinglesChanged = new();

        public class Event_NewThought : UnityEvent<string> { }
        public Event_NewThought NewThought = new();
        #endregion

        public void ResetPlayer()
        {
            HasLaidDown = false;
            State = PlayerState.IDLE;
            PossibleCardCombosChanged.Invoke(new List<CardCombo>());
            PossibleSinglesChanged.Invoke(new List<Single>());
            NewThought.Invoke("<CLEAR>");

            PlayerCardSpotsNode.ResetNode();
            HandCardSpot.ResetSpot();
        }

        private void Update()
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

                    if (laydownCards.Sets.Count == 0)
                    {
                        layStage = LayStage.RUNS;
                        if (laydownCards.Runs.Count == 0)
                            LayingCardsDone();
                    }
                }
            }

            if (State == PlayerState.LAYING)
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
                    currentCardSpot = PlayerCardSpotsNode.AddCardSpot();
                    currentCardSpot.Type = (layStage == LayStage.RUNS) ? CardSpot.SpotType.RUN : CardSpot.SpotType.SET;
                }

                Card card = layStage switch
                {
                    LayStage.SETS => laydownCards.Sets[currentMeldIdx].Cards[currentCardIdx],
                    LayStage.RUNS => laydownCards.Runs[currentMeldIdx].Cards[currentCardIdx],
                    LayStage.SINGLES => singleLayDownCards[currentCardIdx].Card,
                    _ => throw new RummyException("Invalid lay stage: " + layStage)
                };
                HandCardSpot.RemoveCard(card);
                if (!CardsVisible)
                    card.SetTurned(false);
                card.MoveFinished.AddListener(LayDownCardMoveFinished);
                card.MoveCard(currentCardSpot.transform.position, Tb.I.GameMaster.AnimateCardMovement);
            }

            if (State == PlayerState.RETURNING_JOKER)
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
            playerNameHighlight.enabled = true;
            DrawCard(false);
        }

        public void DrawCard(bool isServingCard)
        {
            State = PlayerState.DRAWING;
            bool takeFromDiscardStack = false;
            if (!isServingCard && HasLaidDown)
            {
                // Check if we want to draw from discard stack
                // Note that players will never discard a card which can be added to an already laid-down meld.
                // Therefore, no need to check for that case here.
                Card discardedCard = Tb.I.DiscardStack.TopmostCard();
                var hypotheticalHandCards = new List<Card>(HandCardSpot.Objects) { discardedCard };
                var hypotheticalBestCombo = GetBestCardCombo(hypotheticalHandCards, false);
                int hypotheticalValue = hypotheticalBestCombo.Value;

                int currentValue = GetBestCardCombo(HandCardSpot.Objects, false).Value;

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

        /// <summary>
        /// Rotate the player towards the camera and make sure the text is still readable
        /// </summary>
        internal void Rotate()
        {
            transform.localRotation = Quaternion.Euler(0, 0, 180);
            HandCardSpot.leftToRight = true;
            PlayerCardSpotsNode.leftToRight = true;
            cardCount.GetComponent<RectTransform>().localRotation = Quaternion.Euler(0, 0, 180);
            playerName.GetComponent<RectTransform>().localRotation = Quaternion.Euler(0, 0, 180);
        }

        private void DrawCardFinished(Card card, bool isServingCard)
        {
            card.MoveFinished.RemoveAllListeners();
            HandCardSpot.AddCard(card);
            if (CardsVisible)
                card.SetTurned(false);
            if (isServingCard)
            {
                State = PlayerState.IDLE;
                return;
            }

            var combos = CardUtil.GetAllPossibleCombos(HandCardSpot.Objects, Tb.I.GameMaster.GetAllCardSpotCards(), false);
            PossibleCardCombosChanged.Invoke(combos);
            laydownCards = combos.Count > 0 ? combos[0] : new CardCombo();
            singleLayDownCards = PlayerUtil.UpdateSingleLaydownCards(HandCardSpot.Objects, laydownCards);
            PossibleSinglesChanged.Invoke(singleLayDownCards);

            if (Tb.I.GameMaster.LayingAllowed())
            {
                var usedJokers = false;

                // If the player has not laid down melds yet, check if their sum would be enough to do so
                if (!HasLaidDown)
                {
                    HasLaidDown = laydownCards.Value >= Tb.I.GameMaster.MinimumLaySum;

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
                                laydownCards = combo;
                                combos.Insert(0, combo);
                                PossibleCardCombosChanged.Invoke(combos);
                                NewThought.Invoke("Use jokers to lay down");
                                HasLaidDown = true;
                                break;
                            }
                        }
                        if(!HasLaidDown)
                            NewThought.Invoke("Cannot reach " + Tb.I.GameMaster.MinimumLaySum + " using jokers");
                    }
                }

                // At least one card must remain when laying down
                if (!usedJokers && HasLaidDown && laydownCards.CardCount == HandCardCount)
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

        /// <summary>
        /// Choose one card in <see cref="singleLayDownCards"/> which is kept on hand.
        /// Prioritize cards who do not replace a joker
        /// </summary>
        private void KeepOneSingleCard()
        {
            Single keptSingle = singleLayDownCards.FirstOrDefault(c => c.Joker == null);
            if (keptSingle == null)
                keptSingle = singleLayDownCards[0];
            singleLayDownCards.Remove(keptSingle);
            NewThought.Invoke("Keep single " + keptSingle);
        }

        private void LayDownCardMoveFinished(Card card)
        {
            card.MoveFinished.RemoveAllListeners();
            isCardBeingLaidDown = false;
            currentCardSpot.AddCard(card);

            int cardCount, meldCount;
            switch (layStage)
            {
                case LayStage.SETS:
                    cardCount = laydownCards.Sets[currentMeldIdx].Count;
                    meldCount = laydownCards.Sets.Count;
                    break;
                case LayStage.RUNS:
                    cardCount = laydownCards.Runs[currentMeldIdx].Count;
                    meldCount = laydownCards.Runs.Count;
                    break;
                default: // LayStage.SINGLES
                    cardCount = singleLayDownCards.Count;
                    meldCount = 1;

                    if (!card.IsJoker())
                    {
                        returningJoker = singleLayDownCards[currentCardIdx].Joker;
                        if (returningJoker != null)
                        {
                            State = PlayerState.RETURNING_JOKER;
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

            // All cards of the current meld have been laid down
            currentCardIdx = 0;
            currentMeldIdx++;
            currentCardSpot = null;  // Find a new spot for the next meld

            if (currentMeldIdx < meldCount)
                return;

            // All melds or singles have been laid down
            if (layStage == LayStage.RUNS || layStage == LayStage.SINGLES ||
                (layStage == LayStage.SETS && laydownCards.Runs.Count == 0))
            {
                LayingCardsDone();
            }
            else // LayStage.SETS -> Start laying runs
            {
                currentMeldIdx = 0;
                layStage = LayStage.RUNS;
            }
        }

        private void ReturnJokerMoveFinished(Card joker)
        {
            returningJoker.MoveFinished.RemoveAllListeners();
            isJokerBeingReturned = false;
            HandCardSpot.AddCard(joker);

            if (!CardsVisible)
                joker.SetTurned(true);

            returningJoker = null;

            // All possible runs/sets/singles have to be calculated again with that newly returned joker
            laydownCards = GetBestCardCombo(HandCardSpot.Objects, true);
            singleLayDownCards = PlayerUtil.UpdateSingleLaydownCards(HandCardSpot.Objects, laydownCards);
            PossibleSinglesChanged.Invoke(singleLayDownCards);

            if (laydownCards.CardCount == HandCardCount)
                KeepOneSingleCard();

            // Proceed with waiting
            State = PlayerState.WAITING;
            waitStartTime = Time.time;
        }

        private void LayingCardsDone()
        {
            // With only one card left, just end the game
            if (HandCardCount == 1)
            {
                DiscardCard();
                return;
            }

            // Check if there are any (more) single cards to lay down
            singleLayDownCards = PlayerUtil.UpdateSingleLaydownCards(HandCardSpot.Objects, laydownCards);
            PossibleSinglesChanged.Invoke(singleLayDownCards);

            if (singleLayDownCards.Count == HandCardCount)
                KeepOneSingleCard();

            if (singleLayDownCards.Count > 0)
            {
                currentCardIdx = 0;
                layStage = LayStage.SINGLES;
            }
            else
                DiscardCard();
        }

        private void DiscardCard()
        {
            var thoughts = new List<string>();
            Card card = PlayerUtil.GetCardToDiscard(HandCardSpot.Objects, singleLayDownCards, HasLaidDown, ref thoughts);
            thoughts.ForEach(t => NewThought.Invoke(t));

            State = PlayerState.DISCARDING;
            HandCardSpot.RemoveCard(card);
            if (!CardsVisible)
                card.SetTurned(false);
            card.MoveFinished.AddListener(DiscardCardMoveFinished);
            card.MoveCard(Tb.I.DiscardStack.transform.position, Tb.I.GameMaster.AnimateCardMovement);
        }

        private void DiscardCardMoveFinished(Card card)
        {
            card.MoveFinished.RemoveAllListeners();
            Tb.I.DiscardStack.AddCard(card);

            // Refresh the list of possible card combos and singles for the UI
            GetBestCardCombo(HandCardSpot.Objects, true);
            singleLayDownCards = PlayerUtil.UpdateSingleLaydownCards(HandCardSpot.Objects, laydownCards, true);
            PossibleSinglesChanged.Invoke(singleLayDownCards);

            TurnFinished.Invoke();
            playerNameHighlight.enabled = false;
            State = PlayerState.IDLE;
        }
    }

}