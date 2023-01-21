using UnityEngine;
using UnityEngine.UI;
using rummy.Utility;

namespace rummy.UI.Options
{
    public class ApplyCardSpeed : MonoBehaviour
    {
        public Slider newSpeedSlider;
        public Text currentSpeedText;

        private void Start()
        {
            currentSpeedText.text = Tb.I.GameMaster.CurrentCardMoveSpeed.ToString();
        }

        public void ApplySpeed()
        {
            int newSpeed = (int)newSpeedSlider.value;
            Tb.I.GameMaster.CurrentCardMoveSpeed = newSpeed;
            currentSpeedText.text = newSpeed.ToString();
        }
    }

}