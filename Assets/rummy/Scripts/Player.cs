using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using TMPro;
using rummy.Cards;
using rummy.Utility;

namespace rummy
{

    public abstract class Player : MonoBehaviour
    {
        public UnityEvent<PlayerState> StateChanged = new();
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
        private PlayerState _state = PlayerState.IDLE;
        public PlayerState State
        {
            get => _state;
            protected set
            {
                StateChanged.Invoke(value);
                _state = value;
            }
        }

        public string PlayerName { get; private set; }
        public void SetPlayerName(string name)
        {
            PlayerName = name;
            gameObject.name = name;
            NameChanged.Invoke(name);
        }

        #region PlayerCardSpot
        [SerializeField]
        protected bool CardsVisible = false;
        public void SetCardsVisible(bool visible)
        {
            CardsVisible = visible;
            HandCardSpot.Objects.ForEach(c => c.SetTurned(!visible));
        }

        [SerializeField]
        protected HandCardSpot HandCardSpot;
        public int HandCardCount => HandCardSpot.Objects.Count;
        public int GetHandCardsSum()
        {
            return HandCardSpot.Objects.Sum(c => c.Rank == Card.CardRank.JOKER ? 20 : c.Value);
        }

        [SerializeField]
        protected CardSpotsNode PlayerCardSpotsNode;
        protected CardSpot currentCardSpot;
        public List<CardSpot> GetPlayerCardSpots() => PlayerCardSpotsNode.Objects;
        public int GetLaidCardsSum() => PlayerCardSpotsNode.Objects.Sum(spot => spot.GetValue());

        [SerializeField]
        private TextMeshProUGUI cardCount;
        [SerializeField]
        private TextMeshProUGUI playerName;
        #endregion

        #region CardMelds
        /// <summary>
        /// Whether the player can lay down melds and pick up cards from the discard stack
        /// </summary>
        public bool HasLaidDown { get; protected set; }

        /// <summary>The card sets and runs which are going to be laid down</summary>
        protected CardCombo laydownCardCombo = new();
        /// <summary>The single cards which are going to be laid down</summary>
        protected List<Single> laydownSingles = new();

        protected enum LayStage
        {
            SETS = 0,
            RUNS = 1,
            SINGLES = 2
        }
        protected LayStage layStage = LayStage.SETS;

        protected int currentMeldIdx = 0, currentCardIdx = 0;
        protected bool isCardBeingLaidDown, isJokerBeingReturned;
        #endregion

        #region Events
        /// This player begins the current round (to toggle the underlined name)
        public UnityEvent<bool> IsStarting = new();
        public UnityEvent TurnBegun = new();
        public UnityEvent TurnFinished = new();
        public UnityEvent<string> NameChanged = new();

        public UnityEvent<List<CardCombo>> PossibleCardCombosChanged = new();
        public UnityEvent<List<Single>> PossibleSinglesChanged = new();
        #endregion

        public virtual void ResetPlayer()
        {
            PossibleCardCombosChanged.Invoke(new List<CardCombo>());
            PossibleSinglesChanged.Invoke(new List<Single>());

            HasLaidDown = false;
            State = PlayerState.IDLE;

            PlayerCardSpotsNode.ResetNode();
            HandCardSpot.ResetSpot();
        }

        public virtual void BeginTurn()
        {
            TurnBegun.Invoke();
            DrawCard(false);
        }

        public void DrawCard(bool isServingCard)
        {
            State = PlayerState.DRAWING;

            Card card;
            if(isServingCard)
                card = Tb.I.CardStack.DrawCard();
            else
                card = GetCardToDraw();

            card.MoveFinished.AddListener(c => DrawCardMoveFinished(c, isServingCard));
            card.MoveCard(transform.position, Tb.I.GameMaster.AnimateCardMovement);
        }

        protected abstract Card GetCardToDraw();

        protected virtual void DrawCardMoveFinished(Card card, bool isServingCard)
        {
            card.MoveFinished.RemoveAllListeners();
            HandCardSpot.AddCard(card);
            if (CardsVisible)
                card.SetTurned(false);
            if (isServingCard)
                State = PlayerState.IDLE;
        }

        protected List<CardCombo> UpdatePossibleCombos(List<Card> ignoredCards = null)
        {
            List<Card> cardPool = HandCardSpot.Objects;
            if (ignoredCards != null)
                cardPool = cardPool.Except(ignoredCards).ToList();

            var combos = CardUtil.GetAllPossibleCombos(cardPool, Tb.I.GameMaster.GetAllCardSpotCards(), false);
            PossibleCardCombosChanged.Invoke(combos);
            return combos;
        }

        protected virtual void Update()
        {
            if (State == PlayerState.LAYING && !isCardBeingLaidDown)
            {
                LaydownNextCard();
            }

            if (State == PlayerState.RETURNING_JOKER && !isJokerBeingReturned)
            {
                isJokerBeingReturned = true;
                var joker = laydownSingles[currentCardIdx].Joker;
                currentCardSpot.RemoveCard(joker);
                joker.MoveFinished.AddListener(ReturnJokerMoveFinished);
                joker.MoveCard(HandCardSpot.transform.position, Tb.I.GameMaster.AnimateCardMovement);
            }
        }

        protected void LaydownNextCard()
        {
            isCardBeingLaidDown = true;

            if (layStage == LayStage.SINGLES)
            {
                currentCardSpot = laydownSingles[currentCardIdx].CardSpot;
            }
            else if (currentCardSpot == null)
            {
                currentCardSpot = PlayerCardSpotsNode.AddCardSpot();
                currentCardSpot.Type = (layStage == LayStage.RUNS) ? CardSpot.SpotType.RUN : CardSpot.SpotType.SET;
            }

            Card card = layStage switch
            {
                LayStage.SETS => laydownCardCombo.Sets[currentMeldIdx].Cards[currentCardIdx],
                LayStage.RUNS => laydownCardCombo.Runs[currentMeldIdx].Cards[currentCardIdx],
                LayStage.SINGLES => laydownSingles[currentCardIdx].Card,
                _ => throw new RummyException($"Invalid lay stage: {layStage}")
            };
            HandCardSpot.RemoveCard(card);
            if (!CardsVisible)
                card.SetTurned(false);
            card.MoveFinished.AddListener(LayDownCardMoveFinished);
            card.MoveCard(currentCardSpot.transform.position, Tb.I.GameMaster.AnimateCardMovement);
        }

        protected void LayDownCardMoveFinished(Card card)
        {
            card.MoveFinished.RemoveAllListeners();
            if (layStage == LayStage.SINGLES)
                currentCardSpot.AddCard(laydownSingles[currentCardIdx]);
            else
                currentCardSpot.AddCard(card);
            isCardBeingLaidDown = false;

            int cardCount, meldCount;
            switch (layStage)
            {
                case LayStage.SETS:
                    cardCount = laydownCardCombo.Sets[currentMeldIdx].Count;
                    meldCount = laydownCardCombo.Sets.Count;
                    break;
                case LayStage.RUNS:
                    cardCount = laydownCardCombo.Runs[currentMeldIdx].Count;
                    meldCount = laydownCardCombo.Runs.Count;
                    break;
                default: // LayStage.SINGLES
                    cardCount = laydownSingles.Count;
                    meldCount = 1;

                    if (laydownSingles[currentCardIdx].Joker != null)
                    {
                        State = PlayerState.RETURNING_JOKER;
                        return;
                    }
                    break;
            }

            // Proceed with the next card
            if (currentCardIdx < cardCount - 1)
            {
                currentCardIdx++;
                return;
            }

            // All cards of the current meld have been laid down
            currentCardIdx = 0;
            currentMeldIdx++;
            currentCardSpot = null;  // Find a new spot for the next meld

            // Check if more melds are going to be laid down
            if (currentMeldIdx < meldCount)
                return;

            // Done with sets? Proceed with runs
            if (layStage == LayStage.SETS && laydownCardCombo.Runs.Count > 0)
            {
                currentMeldIdx = 0;
                layStage = LayStage.RUNS;
            }
            else
            {
                LayingCardsDone();
            }
        }

        protected abstract void LayingCardsDone();

        protected virtual void ReturnJokerMoveFinished(Card joker)
        {
            joker.MoveFinished.RemoveAllListeners();
            HandCardSpot.AddCard(joker);

            isJokerBeingReturned = false;
            if (!CardsVisible)
                joker.SetTurned(true);
        }

        protected virtual void DiscardCard(Card card)
        {
            HandCardSpot.RemoveCard(card);
            if (!CardsVisible)
                card.SetTurned(false);
            card.MoveFinished.AddListener(DiscardCardMoveFinished);
            card.MoveCard(Tb.I.DiscardStack.transform.position, Tb.I.GameMaster.AnimateCardMovement);
        }

        protected virtual void DiscardCardMoveFinished(Card card)
        {
            card.MoveFinished.RemoveAllListeners();
            Tb.I.DiscardStack.AddCard(card);

            TurnFinished.Invoke();
            State = PlayerState.IDLE;
        }

        /// <summary>
        /// Rotate a top-row AI player while making sure the cards and text are still readable
        /// </summary>
        internal void Rotate()
        {
            transform.localRotation = Quaternion.Euler(0, 0, 180);
            HandCardSpot.leftToRight = true;
            PlayerCardSpotsNode.leftToRight = true;
            cardCount.GetComponent<RectTransform>().localRotation = Quaternion.Euler(0, 0, 180);
            playerName.GetComponent<RectTransform>().localRotation = Quaternion.Euler(0, 0, 180);
        }
    }

}