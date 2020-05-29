using UnityEngine;
using UnityEngine.UI;
using rummy.Utility;

namespace rummy.UI
{
    [RequireComponent(typeof(Text))]
    public class WaitDurationUI : MonoBehaviour
    {
        private Text text;
        public bool isDrawWaitDuration = false;

        private void Start()
        {
            text = GetComponent<Text>();
        }

        private void Update()
        {
            if (isDrawWaitDuration)
                text.text = Tb.I.GameMaster.DrawWaitDuration.ToString("0.00");
            else
                text.text = Tb.I.GameMaster.PlayWaitDuration.ToString("0.00");
        }
    }

}