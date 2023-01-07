using UnityEngine;
using UnityEngine.UI;
using rummy.Utility;

namespace rummy.UI.Options
{

    public class ApplyNewSeed : MonoBehaviour
    {
        public Text CurrentSeedText;
        public InputField NewSeedInput;

        private void Start()
        {
            CurrentSeedText.text = Tb.I.GameMaster.Seed.ToString();
        }

        public void ApplySeed()
        {
            if (int.TryParse(NewSeedInput.text, out int newSeed))
            {
                Tb.I.GameMaster.Seed = newSeed;
                CurrentSeedText.text = newSeed.ToString();
            }
        }
    }

}