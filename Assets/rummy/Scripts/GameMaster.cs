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
        public List<Player> Players = new List<Player>();
        private Player CurrentPlayer { get { return Players[currentPlayerID]; } }

        private bool isCardBeingDealt;
        private int currentPlayerID;

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

        public class Event_GameOver : UnityEvent<Player> {}
        public Event_GameOver GameOver = new Event_GameOver();

        [SerializeField]
        private CardStack.CardStackType CardStackType = CardStack.CardStackType.DEFAULT;
        [SerializeField]
        private KeyCode PauseKey = KeyCode.P;

        private void Start()
        {
            DefaultGameSpeed = GameSpeed;
            tmpPlayerWaitDuration = PlayWaitDuration;
            tmpDrawWaitDuration = DrawWaitDuration;
            if (SkipUntilRound > 0)
            {
                AnimateCardMovement = false;
                PlayWaitDuration = 0f;
                DrawWaitDuration = 0f;
                GameSpeed = 4;
            }

            Random.InitState(Seed);
            Time.timeScale = GameSpeed;
            RoundCount = 1;

            Tb.I.CardStack.CreateCardStack(CardStackType);
            if (CardStackType != CardStack.CardStackType.CUSTOM)
                Tb.I.CardStack.ShuffleCardStack();

            if (Players.Count == 0)
                Debug.LogError("Missing player references in " + gameObject.name);

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
                else if (CurrentPlayer.playerState == Player.PlayerState.IDLE)
                {
                    isCardBeingDealt = false;
                    currentPlayerID = (currentPlayerID + 1) % Players.Count;

                    if (currentPlayerID == 0 && CurrentPlayer.PlayerCardCount == CardsPerPlayer)
                    {
                        gameState = GameState.PLAYING;
                    }
                }
            }
            else if (gameState == GameState.PLAYING)
            {
                if (CurrentPlayer.playerState == Player.PlayerState.IDLE)
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
            if (CurrentPlayer.PlayerCardCount == 0)
            {
                currentPlayerID = (currentPlayerID + 1) % Players.Count;
                GameOver.Invoke(CurrentPlayer);
                gameState = GameState.NONE;
                return;
            }

            currentPlayerID = (currentPlayerID + 1) % Players.Count;
            if (currentPlayerID == 0)
            {
                RoundCount++;
                if (SkipUntilRound > 0 && RoundCount >= SkipUntilRound && !AnimateCardMovement) //only do this once
                {
                    AnimateCardMovement = true;
                    PlayWaitDuration = tmpPlayerWaitDuration;
                    DrawWaitDuration = tmpDrawWaitDuration;
                    GameSpeed = DefaultGameSpeed;
                }

                if (IsGameADraw())
                {
                    GameOver.Invoke(null);
                    gameState = GameState.NONE;
                    return;
                }
            }

            if (Tb.I.CardStack.CardCount == 0)
            {
                List<Card> discardedCards = Tb.I.DiscardStack.RecycleDiscardedCards();
                Tb.I.CardStack.Restock(discardedCards);
            }

            drawWaitStartTime = Time.time;
            gameState = GameState.DRAWWAIT;
        }

        /// <summary>
        /// Returns whether the current game is a draw and cannot be won by any player
        /// </summary>
        private bool IsGameADraw()
        {
            if (Players.Any(p => p.PlayerCardCount > 2))
                return false;

            foreach (var p in Players)
            {
                var cardSpots = p.GetPlayerCardSpots();
                var setSpots = cardSpots.Where(spot => spot.Type == CardSpot.SpotType.SET);
                var runSpots = cardSpots.Where(spot => spot.Type == CardSpot.SpotType.RUN);

                if (!setSpots.Any() && !runSpots.Any())
                    return false;

                if (setSpots.Any())
                {
                    var fullSetsCount = setSpots.Count(spot => spot.Cards.Count == 4);
                    if (fullSetsCount != setSpots.Count())
                        return false;
                }

                if (runSpots.Any())
                {
                    var fullRunsCount = runSpots.Count(spot => spot.Cards.Count == 14);
                    if (fullRunsCount != runSpots.Count())
                        return false;
                }
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
                cards.AddRange(spot.Cards);
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