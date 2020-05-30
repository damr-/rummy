using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace rummy
{

    public class KeyboardInput : MonoBehaviour
    {
        public Button OptionsButton;
        public Button HideButton;

        private float firstPressTime = -1f;
        private readonly float detectDuration = 1f;

        private void Update()
        {
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
                {
                    firstPressTime = Time.time;
                }
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