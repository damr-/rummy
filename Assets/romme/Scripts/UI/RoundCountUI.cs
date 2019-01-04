using UnityEngine;
using UnityEngine.UI;
using romme.Utility;

namespace romme.UI
{
    [RequireComponent(typeof(Text))]
    public class RoundCountUI : MonoBehaviour
    {
        private Text text;

        private void Start()
        {
            text = GetComponent<Text>();
        }

        private void Update()
        {
            text.text = Tb.I.GameMaster.RoundCount + "";
        }
    }
}