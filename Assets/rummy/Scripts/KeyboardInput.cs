using rummy.UI;
using UnityEngine;
using UnityEngine.UI;

namespace rummy
{

    public class KeyboardInput : MonoBehaviour
    {
        [SerializeField]
        private Button MenuButton;
        [SerializeField]
        private Button CloseMenuButton;
        [SerializeField]
        private Button PauseButton;
        [SerializeField]
        private Button ScoreboardButton;

        private GUIScaler guiScaler;

        private void Start()
        {
            if (MenuButton == null)
                throw new MissingReferenceException($"Missing MenuButton in {gameObject}");
            if (CloseMenuButton == null)
                throw new MissingReferenceException($"Missing CloseMenuButton in {gameObject}");
            if (PauseButton == null)
                throw new MissingReferenceException($"Missing PauseButton in {gameObject}");
            if (PauseButton == null)
                throw new MissingReferenceException($"Missing ScoreboardButton in {gameObject}");
            guiScaler = GetComponent<GUIScaler>();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Plus))
                guiScaler.ChangeScale(true);
            if (Input.GetKeyDown(KeyCode.Minus))
                guiScaler.ChangeScale(false);

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (MenuButton.gameObject.activeInHierarchy)
                    MenuButton.onClick.Invoke();
                else
                    CloseMenuButton.onClick.Invoke();
            }

            if (Input.GetKeyDown(KeyCode.P))
                PauseButton.onClick.Invoke();
            if (Input.GetKeyDown(KeyCode.S))
                ScoreboardButton.onClick.Invoke();
        }

    }

}