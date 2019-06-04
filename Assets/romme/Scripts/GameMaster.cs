using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using romme.Utility;
using romme.Cards;
using System.Linq;
using UniRx;
using System;

namespace romme
{

    public class GameMaster : MonoBehaviour
    {
        public int Seed;
        public float GameSpeed = 1.0f;
        public bool AnimateCardMovement = true;
        public float PlayerWaitDuration = 2f;
        public float PlayerDrawWaitDuration = 2f;
        private float drawWaitStartTime;

        /// <summary>
        /// Disable card movement animation and set the wait durations to 0 until the given round starts. Used for testing. '0' means no round is skipped
        /// </summary>
        public int SkipUntilRound = 0;
        private float tmpPlayerWaitDuration, tmpPlayerDrawWaitDuration, DefaultGameSpeed;

        public int EarliestAllowedLaydownRound = 2;
        public int MinimumLaySum = 40;
        public int CardsPerPlayer = 13;

        public float CardMoveSpeed { get; private set; }
        [SerializeField]
        private float PlayCardSpeed = 50f, DealCardSpeed = 50f;

        public int RoundCount { get; private set; }
        public List<Player> Players = new List<Player>();
        private Player CurPlayer { get { return Players[currentPlayerID]; } }

        private bool isCardBeingDealt;
        private int currentPlayerID;

        private enum GameState
        {
            NONE = 0,
            DEALING = 1,
            PLAYING = 2,
            PAUSED = 3
        }
        private GameState gameState = GameState.NONE;

        private IDisposable playerFinished = Disposable.Empty;

        public IObservable<Player> GameOver { get { return gameOver; } }
        private readonly ISubject<Player> gameOver = new Subject<Player>();

        public CardStack.CardStackType CardStackType = CardStack.CardStackType.DEFAULT;

        public KeyCode PauseKey = KeyCode.P;

        private void Start()
        {
            DefaultGameSpeed = GameSpeed;
            tmpPlayerWaitDuration = PlayerWaitDuration;
            tmpPlayerDrawWaitDuration = PlayerDrawWaitDuration;
            if (SkipUntilRound > 0)
            {
                AnimateCardMovement = false;
                PlayerWaitDuration = 0f;
                PlayerDrawWaitDuration = 0f;
                GameSpeed = 4;
            }

            Extensions.Seed = Seed;
            Time.timeScale = GameSpeed;
            CardMoveSpeed = DealCardSpeed;
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

            if (Mathf.Abs(Time.timeScale - GameSpeed) > Mathf.Epsilon)
                Time.timeScale = GameSpeed;

            if (gameState == GameState.DEALING)
            {
                if (!isCardBeingDealt)
                {
                    isCardBeingDealt = true;
                    CurPlayer.DrawCard(true);
                }
                else if (CurPlayer.playerState == Player.PlayerState.IDLE)
                {
                    isCardBeingDealt = false;
                    currentPlayerID = (currentPlayerID + 1) % Players.Count;

                    if (currentPlayerID == 0 && CurPlayer.PlayerCardCount == CardsPerPlayer)
                    {
                        gameState = GameState.PLAYING;
                        CardMoveSpeed = PlayCardSpeed;
                    }
                }
            }
            else if (gameState == GameState.PLAYING)
            {
                if (CurPlayer.playerState == Player.PlayerState.IDLE)
                {
                    playerFinished = CurPlayer.TurnFinished.Subscribe(PlayerFinished);
                    CurPlayer.BeginTurn();
                }
            }
            else if (gameState == GameState.PAUSED)
            {
                if (Time.time - drawWaitStartTime > PlayerDrawWaitDuration)
                {
                    gameState = GameState.PLAYING;
                }
            }
        }

        private void PlayerFinished(Player player)
        {
            playerFinished.Dispose();

            currentPlayerID = (currentPlayerID + 1) % Players.Count;

            if (player.PlayerCardCount > 0)
            {
                if (currentPlayerID == 0)
                {
                    RoundCount++;
                    if (SkipUntilRound > 0 && RoundCount >= SkipUntilRound && !AnimateCardMovement) //only do this once
                    {
                        AnimateCardMovement = true;
                        PlayerWaitDuration = tmpPlayerWaitDuration;
                        PlayerDrawWaitDuration = tmpPlayerDrawWaitDuration;
                        GameSpeed = DefaultGameSpeed;
                    }
                    bool draw = true;
                    foreach (var p in Players)
                    {
                        var cardSpots = p.GetPlayerCardSpots();
                        var setSpots = cardSpots.Where(spot => spot.Type == CardSpot.SpotType.SET);
                        var runSpots = cardSpots.Where(spot => spot.Type == CardSpot.SpotType.RUN);

                        if (!setSpots.Any() && !runSpots.Any())
                        {
                            draw = false;
                            break;
                        }

                        if (setSpots.Any())
                        {
                            var fullSetsCount = setSpots.Count(spot => spot.Cards.Count == 4);
                            if (fullSetsCount != setSpots.Count())
                            {
                                draw = false;
                                break;
                            }
                        }

                        if (runSpots.Any())
                        {
                            var fullRunsCount = runSpots.Count(spot => spot.Cards.Count == 14);
                            if (fullRunsCount != runSpots.Count())
                            {
                                draw = false;
                                break;
                            }
                        }
                    }

                    if (draw && Players.Any(p => p.PlayerCardCount > 2))
                        draw = false;

                    if (draw)
                    {
                        Debug.Log(Seed + " was a draw!");
                        gameOver.OnNext(null);
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
                gameState = GameState.PAUSED;
            }
            else
            {
                Player otherPlayer = Players[currentPlayerID];
                gameOver.OnNext(otherPlayer);
                gameState = GameState.NONE;
            }
        }

        /// <summary>
        /// Returns the player which is not 'player' or null if the other one was not found
        /// </summary>
        public Player GetOtherPlayer(Player player)
        {
            Player otherPlayer = null;
            foreach (Player p in Players)
            {
                if (p != player)
                    otherPlayer = p;
            }
            return otherPlayer;
        }

        public void RestartGame()
        {
            GameSpeed = DefaultGameSpeed;
            PlayerWaitDuration = tmpPlayerWaitDuration;
            PlayerDrawWaitDuration = tmpPlayerDrawWaitDuration;

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
            GameSpeed = (GameSpeed > 0 ? 0 : DefaultGameSpeed);
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

    }
}