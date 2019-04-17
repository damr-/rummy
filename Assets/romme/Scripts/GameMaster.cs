using System.Collections.Generic;
using UnityEngine;
using romme.Utility;
using UniRx;
using System;

namespace romme
{

    public class GameMaster : MonoBehaviour
    {
        public int Seed;
        public float GameSpeed = 1.0f;
        public int MinimumLaySum = 40;
        public bool AnimateCardMovement = true;

        public float CardMoveSpeed { get; private set; }
        [SerializeField]
        private float PlayCardSpeed = 50f, DealCardSpeed = 50f;

        public int RoundCount { get; private set; }

        public List<Player> Players = new List<Player>();
        private Player CurPlayer { get { return Players[currentPlayerID]; } }

        private bool isCardBeingDealt;
        private int currentPlayerID;

        public enum GameState
        {
            NONE = 0,
            DEALING = 1,
            PLAYING = 2
        }
        public GameState gameState = GameState.NONE;

        private IDisposable playerPlaySub = Disposable.Empty;

        public static readonly int cardsPerPlayer = 13;

        private void Start()
        {
            Extensions.Seed = Seed;
            Time.timeScale = GameSpeed;
            CardMoveSpeed = DealCardSpeed;
            RoundCount = 1;

            Tb.I.CardStack.CreateCardStack();
            Tb.I.CardStack.ShuffleCardStack();

            if (Players.Count == 0)
                Debug.LogError("Missing Players in " + gameObject.name);

            for (int i = 0; i < Players.Count; i++)
                Players[i].PlayerNumber = i;

            gameState = GameState.DEALING;
            currentPlayerID = 0;
        }

        private void Update()
        {
            if(gameState == GameState.DEALING) {
                if (!isCardBeingDealt)
                {
                    isCardBeingDealt = true;
                    CurPlayer.DrawCard(true);
                }
                else if(CurPlayer.playerState == Player.PlayerState.IDLE)
                {
                    isCardBeingDealt = false;
                    currentPlayerID = (currentPlayerID + 1) % Players.Count;

                    if(currentPlayerID == 0 && CurPlayer.PlayerCardCount == cardsPerPlayer)
                    {
                        gameState = GameState.PLAYING;
                        CardMoveSpeed = PlayCardSpeed;
                    }
                }
            }
            else if(gameState == GameState.PLAYING)
            {
                if(CurPlayer.playerState == Player.PlayerState.IDLE)
                {
                    playerPlaySub = CurPlayer.TurnFinished.Subscribe(PlayerFinished);
                    CurPlayer.BeginTurn();
                }
            }
        }

        private void PlayerFinished(Player player)
        {
            playerPlaySub.Dispose();

            currentPlayerID++;
            if(currentPlayerID == Players.Count)
            {
                currentPlayerID = 0;
                RoundCount++;
            }
        }

    }
}