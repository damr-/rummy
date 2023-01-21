using UnityEngine;
using UnityEngine.UI;
using rummy.Utility;

namespace rummy.UI.Options
{

    public class ApplyMinSum : MonoBehaviour
    {
        public Text CurrentSumText;
        public InputField NewMinSumInput;

        private void Start()
        {            
            CurrentSumText.text = Tb.I.GameMaster.MinimumLaySum.ToString();
        }

        public void ApplyNewMinSum()
        {
            if (int.TryParse(NewMinSumInput.text, out int newMinSum))
            {
                if (0 <= newMinSum && newMinSum <= 999)
                {
                    Tb.I.GameMaster.MinimumLaySum = newMinSum;
                    CurrentSumText.text = newMinSum.ToString();
                }
            }
        }
        
    }

}
