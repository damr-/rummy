using UnityEngine;
using UnityEngine.UI;

namespace rummy.UI
{
    [RequireComponent(typeof(GameMaster))]
    public class GameOverUI : MonoBehaviour
    {
        public GameObject gameOverCanvas;
        public Text gameResult;

        /// <summary>
        /// Whether to automatically continue with the next one when a game has ended
        /// </summary>
        public bool continueImmediately;
        public void SetContinueImmediately(bool newValue) => continueImmediately = newValue;

        private GameMaster gameMaster;

        private void Start()
        {
            gameMaster = GetComponent<GameMaster>();
            gameMaster.GameOver.AddListener(GameOver);
        }

        private void Update()
        {
            if (continueImmediately && gameOverCanvas.activeInHierarchy)
            {
                gameMaster.NextGame(false);
                Hide();
            }
        }

        private void GameOver(Player player)
        {
            gameOverCanvas.SetActive(true);
            if (player != null)
                gameResult.text = player.gameObject.name + " won!";
            else
                gameResult.text = "Draw!";
        }

        public void Hide()
        {
            gameOverCanvas.SetActive(false);
        }
    }

}
