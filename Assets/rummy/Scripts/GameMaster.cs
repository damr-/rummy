using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using rummy.Utility;
using rummy.Cards;

namespace rummy
{

    public class GameMaster : MonoBehaviour
    {
        public int Seed;

        public float GameSpeed = 1.0f;
        public void SetGameSpeed(float value) => GameSpeed = value;

        public bool AnimateCardMovement = true;
        public void SetAnimateCardMovement(bool value) => AnimateCardMovement = value;

        private float drawWaitStartTime;
        public float DrawWaitDuration = 2f;
        public void SetDrawWaitDuration(float value) => DrawWaitDuration = value;

        public float PlayWaitDuration = 2f;
        public void SetPlayWaitDuration(float value) => PlayWaitDuration = value;

        public int CardsPerPlayer = 13;
        public int EarliestAllowedLaydownRound = 2;
        public int MinimumLaySum = 40;
        public float CardMoveSpeed = 50f;

        public int RoundCount { get; private set; }
        private List<Player> Players = new List<Player>();
        private Player CurrentPlayer { get { return Players[currentPlayerID]; } }

        private bool isCardBeingDealt;
        private int currentPlayerID;
        private bool skippingDone;

        /// <summary>
        /// FOR DEV PURPOSES ONLY! Disable card movement animation and set the wait durations to 0 until the given round starts. '0' means no round is skipped
        /// </summary>
        public int SkipUntilRound = 0;
        private float tmpPlayerWaitDuration, tmpDrawWaitDuration, DefaultGameSpeed;

        private enum GameState
        {
            NONE = 0,
            DEALING = 1,
            PLAYING = 2,
            DRAWWAIT = 3
        }
        private GameState gameState = GameState.NONE;

        public class Event_GameOver : UnityEvent<Player> { }
        public Event_GameOver GameOver = new Event_GameOver();

        [SerializeField]
        private CardStack.CardStackType CardStackType = CardStack.CardStackType.DEFAULT;
        [SerializeField]
        private KeyCode PauseKey = KeyCode.P;

        private void Start()
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 60;

            if (Players.Count == 0)
                Players = FindObjectsOfType<Player>().OrderBy(p => p.name[p.name.Length - 1]).ToList();

            DefaultGameSpeed = GameSpeed;
            tmpPlayerWaitDuration = PlayWaitDuration;
            tmpDrawWaitDuration = DrawWaitDuration;
            if (SkipUntilRound > 0)
            {
                skippingDone = false;
                AnimateCardMovement = false;
                PlayWaitDuration = 0f;
                DrawWaitDuration = 0f;
                GameSpeed = 4;
            }

            Random.InitState(Seed);
            Time.timeScale = GameSpeed;
            RoundCount = 0;

            Tb.I.CardStack.CreateCardStack(CardStackType);
            if (CardStackType != CardStack.CardStackType.CUSTOM)
                Tb.I.CardStack.ShuffleCardStack();

            gameState = GameState.DEALING;
            currentPlayerID = 0;
        }

        private void Update()
        {
            if (Input.GetKeyDown(PauseKey))
                TogglePause();
            Time.timeScale = GameSpeed;

            if (gameState == GameState.DEALING)
            {
                if (!isCardBeingDealt)
                {
                    isCardBeingDealt = true;
                    CurrentPlayer.DrawCard(true);
                }
                else if (CurrentPlayer.State == Player.PlayerState.IDLE)
                {
                    isCardBeingDealt = false;
                    currentPlayerID = (currentPlayerID + 1) % Players.Count;

                    if (currentPlayerID == 0 && CurrentPlayer.HandCardCount == CardsPerPlayer)
                    {
                        gameState = GameState.PLAYING;
                        RoundCount = 1;
                        TryStopSkipping();
                    }
                }
            }
            else if (gameState == GameState.PLAYING)
            {
                if (CurrentPlayer.State == Player.PlayerState.IDLE)
                {
                    CurrentPlayer.TurnFinished.AddListener(PlayerFinished);
                    CurrentPlayer.BeginTurn();
                }
            }
            else if (gameState == GameState.DRAWWAIT)
            {
                if (Time.time - drawWaitStartTime > DrawWaitDuration)
                    gameState = GameState.PLAYING;
            }
        }

        private void PlayerFinished()
        {
            CurrentPlayer.TurnFinished.RemoveAllListeners();
            if (CurrentPlayer.HandCardCount == 0)
            {
                GameOver.Invoke(CurrentPlayer);
                gameState = GameState.NONE;
                return;
            }

            currentPlayerID = (currentPlayerID + 1) % Players.Count;
            if (currentPlayerID == 0)
            {
                RoundCount++;
                TryStopSkipping();

                if (IsGameADraw())
                {
                    GameOver.Invoke(null);
                    gameState = GameState.NONE;
                    return;
                }
            }

            if (Tb.I.CardStack.CardCount == 0)
            {
                var discardedCards = Tb.I.DiscardStack.RecycleDiscardedCards();
                Tb.I.CardStack.Restock(discardedCards);
            }

            drawWaitStartTime = Time.time;
            gameState = GameState.DRAWWAIT;
        }

        private void TryStopSkipping()
        {
            if (skippingDone || SkipUntilRound <= 0 || RoundCount < SkipUntilRound)
                return;
            skippingDone = true;
            AnimateCardMovement = true;
            PlayWaitDuration = tmpPlayerWaitDuration;
            DrawWaitDuration = tmpDrawWaitDuration;
            GameSpeed = DefaultGameSpeed;
        }

        /// <summary>
        /// Returns whether all players in the game have fewer than 'count' cards on their hands each.
        /// </summary>
        /// <param name="count">The number of cards which is checked against</param>
        /// <returns>True if all players have less than 'count' cards on their hands each, false otherwise.</returns>
        public bool AllPlayersHaveFewerCardsThan(int count)
        {
            return Players.All(p => p.HandCardCount < count);
        }

        /// <summary>
        /// Returns whether the current game is a draw and cannot be won by any player
        /// </summary>
        private bool IsGameADraw()
        {
            if (!AllPlayersHaveFewerCardsThan(3))
                return false;

            foreach (var p in Players)
            {
                var cardSpots = p.GetPlayerCardSpots();
                if (cardSpots.Any(spot => !spot.IsFull()))
                    return false;
            }
            return true;
        }

        public void RestartGame()
        {
            if (SkipUntilRound > 0)
            {
                GameSpeed = DefaultGameSpeed;
                DrawWaitDuration = tmpDrawWaitDuration;
                PlayWaitDuration = tmpPlayerWaitDuration;
            }

            Tb.I.CardStack.ResetStack();
            Tb.I.DiscardStack.ResetStack();
            Players.ForEach(p => p.ResetPlayer());
            Start();
        }

        public void NextGame()
        {
            Seed += 1;
            RestartGame();
        }

        public void TogglePause()
        {
            if (GameSpeed > 0)
            {
                DefaultGameSpeed = GameSpeed;
                GameSpeed = 0;
            }
            else
            {
                GameSpeed = DefaultGameSpeed;
            }
        }

        public List<CardSpot> GetAllCardSpots()
        {
            var cardSpots = new List<CardSpot>();
            foreach (var player in Players)
                cardSpots.AddRange(player.GetPlayerCardSpots());
            return cardSpots;
        }

        public List<Card> GetAllCardSpotCards()
        {
            var cards = new List<Card>();
            var cardSpots = GetAllCardSpots();
            foreach (var spot in cardSpots)
                cards.AddRange(spot.Objects);
            return cards;
        }

        public void LogMsg(string message, LogType type)
        {
            string prefix = "[Seed " + Seed + ", Round " + RoundCount + "] ";
            switch (type)
            {
                case LogType.Error:
                    Debug.LogError(prefix + message);
                    break;
                case LogType.Warning:
                    Debug.LogWarning(prefix + message);
                    break;
                default:
                    Debug.Log(prefix + message);
                    break;
            }
        }
    }

}