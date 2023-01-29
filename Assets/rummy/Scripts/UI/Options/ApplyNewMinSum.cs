using UnityEngine;
using UnityEngine.UI;

namespace rummy.UI.Options
{

    public class ApplyNewMinSum : MonoBehaviour
    {
        public InputField NewMinSumInput;
        private GameMaster GameMaster;

        private void Start()
        {
            GameMaster = GetComponentInParent<GameMaster>();
        }

        public void Apply()
        {
            if (int.TryParse(NewMinSumInput.text, out int newMinSum))
            {
                if (0 <= newMinSum && newMinSum <= 135)
                {
                    GameMaster.SetMinimumLaySum(newMinSum);
                }
            }
        }
        
    }

}
