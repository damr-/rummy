using UnityEngine;
using UnityEngine.UI;
using romme.Utility;

namespace romme.UI.Options
{

    public class ApplyNewSeed : MonoBehaviour
    {
        public InputField NewSeedInput;
        public void ApplySeed()
        {
            int newSeed = 0;
            int.TryParse(NewSeedInput.text, out newSeed);
            Tb.I.GameMaster.Seed = newSeed;
        }
    }

}