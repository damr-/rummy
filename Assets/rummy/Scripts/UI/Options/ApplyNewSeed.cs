using UnityEngine;
using UnityEngine.UI;

namespace rummy.UI.Options
{

    public class ApplyNewSeed : MonoBehaviour
    {
        public InputField NewSeedInput;
        private GameMaster GameMaster;

        private void Start()
        {
            GameMaster = GetComponentInParent<GameMaster>();
        }

        public void Apply()
        {
            if (int.TryParse(NewSeedInput.text, out int newSeed))
            {
                GameMaster.SetSeed(newSeed);
            }
        }
    }

}