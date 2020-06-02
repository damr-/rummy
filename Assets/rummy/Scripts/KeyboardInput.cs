using rummy.UI;
using rummy.Utility;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace rummy
{

    public class KeyboardInput : MonoBehaviour
    {
        public Button OptionsButton;
        public Button HideButton;
        public GUIScaler guiScaler;

        private float firstPressTime = -1f;
        private readonly float detectDuration = 1f;

        private void Start()
        {
            if (OptionsButton == null)
                throw new MissingReferenceException("Missing OptionsButton in " + gameObject);
            if (HideButton == null)
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

            if (Input.GetKeyDown(KeyCode.O))
            {
                if (OptionsButton.gameObject.activeInHierarchy)
                    OptionsButton.onClick.Invoke();
                else
                    HideButton.onClick.Invoke();
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (firstPressTime < 0)
                    firstPressTime = Time.time;
                else
                {
                    if (Time.time - firstPressTime < detectDuration)
                        QuitApp();
                    else
                        firstPressTime = -1;
                }
            }
        }

        public void QuitApp()
        {
#if UNITY_EDITOR
            EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }

}