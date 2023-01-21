using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using rummy.Utility;
using rummy.Cards;
using rummy.UI;

namespace rummy
{

    public class GameMaster : MonoBehaviour
    {
        public bool StartWithRandomSeed = true;
        public int Seed;

        public float GameSpeed = 1.0f;
        public void SetGameSpeed(float value) => GameSpeed = value;

        public bool AnimateCardMovement = true;
        public void SetAnimateCardMovement(bool value) => AnimateCardMovement = value;

        private float drawWaitStartTime;
        public float DrawWaitDuration = 0.5f;
        public void SetDrawWaitDuration(float value) => DrawWaitDuration = value;

        public float PlayWaitDuration = 1f;
        public void SetPlayWaitDuration(float value) => PlayWaitDuration = value;

        public int CardsPerPlayer = 13;
        public int EarliestLaydownRound = 2;
        public int MinimumLaySum = 40;
        public float PlayCardMoveSpeed = 50f;
        public float DealCardMoveSpeed = 200f;
        public float CurrentCardMoveSpeed = 0f;

        public int RoundCount { get; private set; }
        /// <summary> Returns whether laying down cards in the current round is allowed </summary>
        public bool LayingAllowed() => RoundCount >= EarliestLaydownRound;
        private List<Player> Players = new();
        private Player CurrentPlayer => Players[currentPlayerID];

        public static List<string> PlayerNamePool = new() {
            "Agnes", "Alfred", "Archy", "Barty", "Benjamin", "Bertram", "Bruni",
            "Buster", "Edith", "Ester", "Flo", "Francis", "Francisco", "Gil",
            "Gob", "Gus", "Hank", "Harold", "Harriet", "Henry", "Jacques",
            "Jorge", "Juan", "Kitty", "Lionel", "Louie", "Lucille", "Lupe",
            "Mabel", "Maeby", "Marco", "Marta", "Maurice", "Maynard",
            "Mildred", "Monty", "Mordecai", "Morty", "Pablo", "Seymour",
            "Stan", "Tobias", "Vivian", "Walter", "Wilbur"};

        private bool isCardBeingDealt;
        private int currentPlayerID;
        private int currentStartingPlayerID = -1;
        private bool skippingDone;

        /// <summary>
        /// Quickly forward until the set round starts. '0' means no round is skipped
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
        public Event_GameOver GameOver = new();

        [SerializeField]
        private CardStack.CardStackType CardStackType = CardStack.CardStackType.DEFAULT;

        public Scoreboard Scoreboard;

        private void Start()
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 60;

            if(StartWithRandomSeed)
                Seed = Random.Range(0, int.MaxValue);
            Random.InitState(Seed);

            Players = FindObjectsOfType<Player>().ToList();
            List<string> usedNames = new();
            foreach (var p in Players)
            {
                string playerName;
                do
                {
                    playerName = PlayerNamePool.ElementAt(Random.Range(0, PlayerNamePool.Count));
                } while (usedNames.Contains(playerName));
                p.SetPlayerName(playerName);
                usedNames.Add(playerName);
            }
            Scoreboard.AddPlayerNamesLine(Players);

            Tb.I.CardStack.CreateCardStack(CardStackType);
            StartGame();
        }

        private void StartGame()
        {
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

            Time.timeScale = GameSpeed;
            RoundCount = 0;

            gameState = GameState.DEALING;
            CurrentCardMoveSpeed = DealCardMoveSpeed;
            currentStartingPlayerID = (currentStartingPlayerID + 1) % Players.Count;
            currentPlayerID = currentStartingPlayerID;
        }

        public void NextGame()
        {
            Seed += 1;
            RestartGame();
        }

        public void RestartGame()
        {
            if (SkipUntilRound > 0)
            {
                GameSpeed = DefaultGameSpeed;
                DrawWaitDuration = tmpDrawWaitDuration;
                PlayWaitDuration = tmpPlayerWaitDuration;
            }

            var cards = new List<Card>();
            cards.AddRange(Tb.I.DiscardStack.RemoveCards());
            foreach (var p in Players)
                cards.AddRange(p.ResetPlayer());

            Random.InitState(Seed);
            Tb.I.CardStack.Restock(cards, true);
            StartGame();
        }

        private void Update()
        {
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
                    if (currentPlayerID == currentStartingPlayerID &&
                        CurrentPlayer.HandCardCount == CardsPerPlayer)
                    {
                        gameState = GameState.PLAYING;
                        CurrentCardMoveSpeed = PlayCardMoveSpeed;
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
                WriteScores();
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
                    WriteScores();
                    GameOver.Invoke(null);
                    gameState = GameState.NONE;
                    return;
                }
            }

            if (Tb.I.CardStack.CardCount == 0)
            {
                var discardedCards = Tb.I.DiscardStack.RecycleDiscardedCards();
                Tb.I.CardStack.Restock(discardedCards, false);
            }

            drawWaitStartTime = Time.time;
            gameState = GameState.DRAWWAIT;
        }

        private void WriteScores()
        {
            Scoreboard.AddScoreLine(Players);
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
        /// Returns whether the current game is a draw and cannot be won by any player
        /// </summary>
        private bool IsGameADraw()
        {
            if (Players.Any(p => p.HandCardCount >= 3))
                return false;

            foreach (var p in Players)
            {
                var cardSpots = p.GetPlayerCardSpots();
                if (cardSpots.Any(spot => !spot.IsFull(true)))
                    return false;
            }
            return true;
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

        public void Log(string message, LogType type)
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