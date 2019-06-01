using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UniRx;
using romme.Utility;

namespace romme.UI
{
    [RequireComponent(typeof(GameMaster))]
    public class GameOverUI : MonoBehaviour
    {
        public GameObject gameOverCanvas;
        public Text loserCardValue;
        [Tooltip("Enable to automatically continue with the next game when a game has ended")]
        public bool continueImmediately;

        private void Start()
        {
            GetComponent<GameMaster>().GameOver.Subscribe(GameOver);
        }

        private void Update()
        {
            if(continueImmediately && gameOverCanvas.activeInHierarchy)
            {
                Tb.I.GameMaster.NextGame();
                Hide();
            }
        }

        private void GameOver(int cardValue)
        {
            gameOverCanvas.SetActive(true);
            loserCardValue.text = "Loser card value: " + cardValue;
        }

        public void Hide()
        {
            gameOverCanvas.SetActive(false);
        }
    }

}
