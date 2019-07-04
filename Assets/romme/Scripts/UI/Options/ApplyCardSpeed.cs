using UnityEngine;
using UnityEngine.UI;
using romme.Utility;

namespace romme.UI.Options
{
    public class ApplyCardSpeed : MonoBehaviour
    {
        public Slider newSpeedSlider;
        public Text currentSpeedText;

        private void Start()
        {
            currentSpeedText.text = Tb.I.GameMaster.CardMoveSpeed.ToString();
        }

        public void ApplySpeed()
        {
            int newSpeed = (int)newSpeedSlider.value;
            Tb.I.GameMaster.CardMoveSpeed = newSpeed;
            currentSpeedText.text = newSpeed.ToString();
        }
    }

}