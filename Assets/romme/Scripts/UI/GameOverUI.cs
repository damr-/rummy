using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UniRx;

namespace romme.UI
{
    [RequireComponent(typeof(GameMaster))]
    public class GameOverUI : MonoBehaviour
    {
        public GameObject gameOverCanvas;
        public Text loserCardValue;

        private void Start()
        {
            GetComponent<GameMaster>().GameOver.Subscribe(GameOver);
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
