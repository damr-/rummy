using UnityEngine;
using UnityEngine.UI;
using rummy.Utility;

namespace rummy.UI.Options
{

    public class ApplyNewSeed : MonoBehaviour
    {
        public InputField NewSeedInput;
        public void ApplySeed()
        {
            int.TryParse(NewSeedInput.text, out int newSeed);
            Tb.I.GameMaster.Seed = newSeed;
        }
    }

}