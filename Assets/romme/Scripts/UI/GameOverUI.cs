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
        [Tooltip("Enable to automatically continue with the next game when a game has ended")]
        public bool continueImmediately;
        public void SetContinueImmediately(bool newValue) => continueImmediately = newValue;

        private GameMaster gameMaster;

        private void Start()
        {
            gameMaster = GetComponent<GameMaster>();
            gameMaster.GameOver.Subscribe(GameOver);
        }

        private void Update()
        {
            if (continueImmediately && gameOverCanvas.activeInHierarchy)
            {
                gameMaster.NextGame();
                Hide();
            }
        }

        private void GameOver(Player player)
        {
            gameOverCanvas.SetActive(true);
            loserCardValue.text = "Loser card value: " + (player == null ? "0" : player.PlayerHandValue.ToString());
        }

        public void Hide()
        {
            gameOverCanvas.SetActive(false);
        }
    }

}
