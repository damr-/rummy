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
        public PlayerState State { get; protected set; } = PlayerState.IDLE;

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

        #region CardSets
        /// <summary>
        /// Whether the player can lay down melds and pick up cards from the discard stack
        /// </summary>
        public bool HasLaidDown { get; protected set; }
        #endregion

        #region Events
        /// This player begins the current round (to toggle the underlined name)
        public UnityEvent<bool> IsStarting = new();
        public UnityEvent TurnBegun = new();
        public UnityEvent TurnFinished = new();
        public UnityEvent<string> NameChanged = new();
        #endregion

        public virtual void ResetPlayer()
        {
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

        public abstract void DrawCard(bool isServingCard);
        protected abstract void DrawCardFinished(Card card, bool isServingCard);

        protected virtual void LayDownCardMoveFinished(Card card)
        {
            card.MoveFinished.RemoveAllListeners();
            currentCardSpot.AddCard(card);
        }

        protected virtual void ReturnJokerMoveFinished(Card joker)
        {
            joker.MoveFinished.RemoveAllListeners();
            HandCardSpot.AddCard(joker);
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