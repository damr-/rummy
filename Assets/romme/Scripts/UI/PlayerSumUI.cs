using UnityEngine;
using UnityEngine.UI;

namespace romme.UI
{
    [RequireComponent(typeof(Text))]
    public class PlayerSumUI : MonoBehaviour
    {
        public Player Player;
        private Text text;

        private void Start()
        {
            text = GetComponent<Text>();
        }

        private void Update()
        {
            text.text = Player.GetLaidCardsSum().ToString();
        }
    }
}
