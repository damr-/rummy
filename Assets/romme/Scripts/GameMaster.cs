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
        /// Don't animate card movement and set the wait durations to 0 until the given round starts. Used for testing. '0' means no round is skipped
        /// </summary>
        public int SkipUntilRound = 0;
        private float tmpPlayerWaitDuration, tmpPlayerDrawWaitDuration, tmpGameSpeed;

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

        public IObservable<int> GameOver { get { return gameOver; } }
        private readonly ISubject<int> gameOver = new Subject<int>();

        public enum TEST_CardServeType
        {
            DEFAULT = 0,
            NO_JOKER = 1,
            ONLY_JACKS = 2,
            ONLY_HEARTS = 3
        }
        public TEST_CardServeType CardServeType = TEST_CardServeType.DEFAULT;

        private void Start()
        {
            if(SkipUntilRound > 0)
            {
                AnimateCardMovement = false;                
                tmpPlayerWaitDuration = PlayerWaitDuration;
                tmpPlayerDrawWaitDuration = PlayerDrawWaitDuration;
                tmpGameSpeed = GameSpeed;;
                PlayerWaitDuration = 0f;
                PlayerDrawWaitDuration = 0f;
                GameSpeed = 4;
            }

            Extensions.Seed = Seed;
            Time.timeScale = GameSpeed;
            CardMoveSpeed = DealCardSpeed;
            RoundCount = 1;

            switch (CardServeType)
            {
                case TEST_CardServeType.DEFAULT:
                    Tb.I.CardStack.CreateCardStack();
                    break;
                case TEST_CardServeType.NO_JOKER:
                    Tb.I.CardStack.CreateCardStackNoJoker();
                    break;
                case TEST_CardServeType.ONLY_JACKS:
                    Tb.I.CardStack.TEST_CreateJackCardStack();
                    break;
                default: // TEST_CardServeType.ONLY_HEARTS:
                    Tb.I.CardStack.TEST_CreateHeartCardStack();
                    break;
            }
            Tb.I.CardStack.ShuffleCardStack();

            if (Players.Count == 0)
                Debug.LogError("Missing player references in " + gameObject.name);

            gameState = GameState.DEALING;
            currentPlayerID = 0;
        }

        private void Update()
        {
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
                if(Time.time - drawWaitStartTime > PlayerDrawWaitDuration)
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
                    if(SkipUntilRound > 0 && RoundCount == SkipUntilRound && !AnimateCardMovement) //only do this once
                    {
                        AnimateCardMovement = true;
                        PlayerWaitDuration = tmpPlayerWaitDuration;
                        PlayerDrawWaitDuration = tmpPlayerDrawWaitDuration;
                        GameSpeed = tmpGameSpeed;
                    }
                }

                if (Tb.I.CardStack.CardCount == 0)
                {
                    List<Card> discardedCards = Tb.I.DiscardStack.RemoveCards();
                    Tb.I.CardStack.Restock(discardedCards);
                }

                drawWaitStartTime = Time.time;
                gameState = GameState.PAUSED;
            }
            else
            {
                Player otherPlayer = Players[currentPlayerID];
                gameOver.OnNext(otherPlayer.PlayerHandValue);
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
            Tb.I.CardStack.ResetStack();
            Tb.I.DiscardStack.ResetStack();
            foreach (Player p in Players)
                p.ResetPlayer();
            Start();
        }

    }
}