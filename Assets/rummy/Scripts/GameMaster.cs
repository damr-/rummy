using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using rummy.Cards;
using rummy.UI;

namespace rummy
{

    public class GameMaster : MonoBehaviour
    {
        [SerializeField]
        private int CardsPerPlayer = 13;
        [SerializeField]
        private int EarliestLaydownRound = 2;
        public int MinimumLaySum { get; private set; } = 40;
        public void SetMinimumLaySum(int value) => MinimumLaySum = value;

        [SerializeField]
        private bool StartWithRandomSeed = true;
        public int Seed;
        public void SetSeed(int value) => Seed = value;

        private float DefaultGameSpeed; // Stored during pause
        public float GameSpeed { get; private set; } = 1.0f;
        public void SetGameSpeed(float value) => GameSpeed = value;

        public bool AnimateCardMovement { get; private set; } = true;
        public void SetAnimateCardMovement(bool value) => AnimateCardMovement = value;

        private float drawWaitStartTime;
        public float DrawWaitDuration { get; private set; } = 0.5f;
        public void SetDrawWaitDuration(float value) => DrawWaitDuration = value;

        public float PlayWaitDuration { get; private set; } = 1f;
        public void SetPlayWaitDuration(float value) => PlayWaitDuration = value;

        [SerializeField]
        private float PlayCardMoveSpeed = 50f;
        [SerializeField]
        private float DealCardMoveSpeed = 200f;
        public float CurrentCardMoveSpeed { get; private set; }
        public void SetCurrentCardMoveSpeed(float value) => CurrentCardMoveSpeed = value;

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

        [SerializeField]
        private int SkipUntilRound = 0; // Quickly forward until the given round starts. '0': no round is skipped
        private bool skippingDone;
        private float storedPlayerWaitDur, storedDrawWaitDur, storedGameSpeed;

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

        public CardStack CardStack;
        public DiscardStack DiscardStack;
        [SerializeField]
        private CardStack.CardStackType CardStackType = CardStack.CardStackType.DEFAULT;

        private Scoreboard Scoreboard;

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
            Scoreboard = GetComponentInChildren<Scoreboard>(true);
            Scoreboard.AddPlayerNamesLine(Players);

            CardStack.CreateCardStack(CardStackType);
            StartGame();
        }

        private void StartGame()
        {
            if (SkipUntilRound > 0)
            {
                storedGameSpeed = GameSpeed;
                storedPlayerWaitDur = PlayWaitDuration;
                storedDrawWaitDur = DrawWaitDuration;

                skippingDone = false;
                AnimateCardMovement = false;
                PlayWaitDuration = 0f;
                DrawWaitDuration = 0f;
                GameSpeed = 10;
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
                GameSpeed = storedGameSpeed;
                PlayWaitDuration = storedPlayerWaitDur;
                DrawWaitDuration = storedDrawWaitDur;
            }

            var cards = new List<Card>();
            cards.AddRange(DiscardStack.RemoveCards());
            foreach (var p in Players)
                cards.AddRange(p.ResetPlayer());

            Random.InitState(Seed);
            CardStack.Restock(cards, true);
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

            if (CardStack.CardCount == 0)
            {
                var discardedCards = DiscardStack.RecycleDiscardedCards();
                CardStack.Restock(discardedCards, false);
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
            if (!skippingDone && RoundCount == SkipUntilRound)
            {
                skippingDone = true;
                AnimateCardMovement = true;
                GameSpeed = storedGameSpeed;
                PlayWaitDuration = storedPlayerWaitDur;
                DrawWaitDuration = storedDrawWaitDur;
            }
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