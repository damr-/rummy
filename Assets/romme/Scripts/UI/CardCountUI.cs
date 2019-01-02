using UnityEngine;
using UnityEngine.UI;

namespace romme.UI
{
    [RequireComponent(typeof(Text))]
    public class CardCountUI : MonoBehaviour
    {
        public Player Player;
        private Text text;

        private void Start()
        {
            text = GetComponent<Text>();
            if (Player == null)
                Debug.LogError(gameObject.name + " missing player reference!");
        }

        private void Update()
        {
            text.text = Player.PlayerCardCount + "";
        }

    }

}