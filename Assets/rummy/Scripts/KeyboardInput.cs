using rummy.UI;
using rummy.Utility;
using UnityEngine;
using UnityEngine.UI;

namespace rummy
{

    public class KeyboardInput : MonoBehaviour
    {
        public Button MenuButton;
        public Button CloseMenuButton;
        public GUIScaler guiScaler;

        private void Start()
        {
            if (MenuButton == null)
                throw new MissingReferenceException("Missing OptionsButton in " + gameObject);
            if (CloseMenuButton == null)
                throw new MissingReferenceException("Missing HideButton in " + gameObject);
            if (guiScaler == null)
                throw new MissingReferenceException("Missing guiScaler in " + gameObject);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.P))
                Tb.I.GameMaster.TogglePause();

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
        }

    }

}