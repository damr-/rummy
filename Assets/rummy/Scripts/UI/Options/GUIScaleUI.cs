using UnityEngine;
using UnityEngine.UI;

namespace rummy.UI.Options
{
    [RequireComponent(typeof(Text))]
    public class GUIScaleUI : MonoBehaviour
    {
        private Text text;
        public float GUIScale = 1.0f;

        private void Start()
        {
            text = GetComponent<Text>();
        }

        public void Update()
        {
            text.text = GUIScale.ToString("0.00");
        }
    }

}