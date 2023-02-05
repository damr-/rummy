using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using rummy.Cards;
using rummy.UI;
using rummy.Utility;

namespace rummy
{

    public class GameMaster : MonoBehaviour
    {
        public int PlayerCount = 2;
        public Transform PlayersParent;
        public GameObject PlayerPrefab;
        public void ChangePlayerCount(bool increase)
        {
            if (increase) {
                if (PlayerCount < 6)
                    PlayerCount += 1;
            }
            else if (PlayerCount > 2)
                PlayerCount -= 1;
        }
        private static readonly int PLAYER_X = 15;
        private static readonly int PLAYER_Y = 14;
        private static readonly Vector3 LD = new(-PLAYER_X, -PLAYER_Y, 0);
        private static readonly Vector3 LU = new(-PLAYER_X,  PLAYER_Y, 0);
        private static readonly Vector3 CD = new(0, -PLAYER_Y, 0);
        private static readonly Vector3 CU = new(0, PLAYER_Y, 0);
        private static readonly Vector3 RD = new(PLAYER_X, -PLAYER_Y, 0);
        private static readonly Vector3 RU = new(PLAYER_X,  PLAYER_Y, 0);

        private readonly IDictionary<int, List<Vector3>> PlayerPos = new Dictionary<int, List<Vector3>>()
        {
            {2, new List<Vector3> { CD, CU } },
            {3, new List<Vector3> { CD, LU, RU } },
            {4, new List<Vector3> { CD, LU, CU, RU } },
            {5, new List<Vector3> { CD, LD, LU, RU, RD } },
            {6, new List<Vector3> { CD, LD, LU, CU, RU, RD } }
        };

        [SerializeField]
        private int CardsPerPlayer = 13;
        [SerializeField]
        private int EarliestLaydownRound = 2;
        public int MinimumLaySum { get; private set; } = 40;
        public void SetMinimumLaySum(int value) => MinimumLaySum = value;

        [SerializeField]
        private bool RandomizeSeed = true;
        public int Seed;
        public void SetSeed(int value) => Seed = value;
        [SerializeField]
        private int startingPlayer = 0;

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

        public float CurrentCardMoveSpeed => gameState == GameState.DEALING ? DealCardMoveSpeed : PlayCardMoveSpeed;
        public float PlayCardMoveSpeed { get; private set; } = 30f;
        public float DealCardMoveSpeed { get; private set; } = 200f;
        public void SetPlayCardMoveSpeed(float value) => PlayCardMoveSpeed = value;
        public void SetDealCardMoveSpeed(float value) => DealCardMoveSpeed = value;

        public int RoundCount { get; private set; }
        /// <summary> Returns whether laying down cards in the current round is allowed </summary>
        public bool LayingAllowed() => RoundCount >= EarliestLaydownRound;

        public readonly List<Player> Players = new();
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
        private int currentStartingPlayerID;

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

        [SerializeField]
        private CardStack CardStack;
        [SerializeField]
        private DiscardStack DiscardStack;
        [SerializeField]
        private CardStack.CardStackType CardStackType = CardStack.CardStackType.DEFAULT;

        private Scoreboard Scoreboard;

        private void Start()
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 60;

            if (RandomizeSeed)
                Seed = Random.Range(0, int.MaxValue);

            Scoreboard = GetComponentInChildren<Scoreboard>(true);
            StartGame(true);
        }

        private void StartGame(bool newGame)
        {
            Random.InitState(Seed);
            CardStack.CreateCardStack(CardStackType);

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

            if (newGame)
            {
                Players.ClearAndDestroy();
                currentStartingPlayerID = startingPlayer - 1;
                CreatePlayers();
            }

            gameState = GameState.DEALING;
            if (currentStartingPlayerID >= 0)
                Players[currentStartingPlayerID].IsStarting.Invoke(false);
            currentStartingPlayerID = (currentStartingPlayerID + 1) % Players.Count;
            currentPlayerID = currentStartingPlayerID;
            CurrentPlayer.IsStarting.Invoke(true);
        }

        private void CreatePlayers()
        {
            List<string> usedNames = new();
            while (Players.Count < PlayerCount)
            {
                var p = Instantiate(PlayerPrefab, PlayersParent).GetComponent<Player>();
                Players.Add(p);

                string playerName;
                do
                {
                    playerName = PlayerNamePool.ElementAt(Random.Range(0, PlayerNamePool.Count));
                } while (usedNames.Contains(playerName));

                p.SetPlayerName(playerName);
                usedNames.Add(playerName);
            }

            //Update player positions for current player count
            List<Vector3> pos = PlayerPos[Players.Count];
            for (int i = 0; i < Players.Count; i++)
            {
                var p = Players[i];
                p.transform.position = pos[i];
                if (pos[i].y > 0) // Rotate the players in the top row
                    p.Rotate();
            }

            Scoreboard.Clear();
            Scoreboard.AddLine(Players, true);
        }

        /// <summary>
        /// Change the seed and restart the game
        /// </summary>
        /// <param name="newGame"></param>
        public void NextGame(bool newGame)
        {
            if (RandomizeSeed)
            {
                int prevSeed = Seed;
                do
                {
                    Seed = Random.Range(0, int.MaxValue);
                } while (Seed == prevSeed);
            }
            else
                Seed += 1;

            RestartGame(newGame);
        }

        /// <summary>
        /// Remove all cards and start a new game
        /// </summary>
        /// <param name="newGame"></param>
        public void RestartGame(bool newGame)
        {
            gameState = GameState.NONE;
            if (SkipUntilRound > 0)
            {
                GameSpeed = storedGameSpeed;
                PlayWaitDuration = storedPlayerWaitDur;
                DrawWaitDuration = storedDrawWaitDur;
            }

            DiscardStack.RemoveCards();
            foreach (var p in Players)
                p.ResetPlayer();
            FindObjectsOfType<Card>().ToList().ClearAndDestroy();

            StartGame(newGame);
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
                Scoreboard.AddLine(Players, false);
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
                    Scoreboard.AddLine(Players, false);
                    GameOver.Invoke(null);
                    gameState = GameState.NONE;
                    return;
                }
            }

            if (CardStack.CardCount == 0)
            {
                var discardedCards = DiscardStack.RecycleDiscardedCards();
                CardStack.Restock(discardedCards);
            }

            drawWaitStartTime = Time.time;
            gameState = GameState.DRAWWAIT;
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
        /// Return whether the current game is a draw and cannot be won by any player
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