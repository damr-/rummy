using UnityEngine;
using UnityEngine.UI;
using romme.Cards;

namespace romme.UI
{
    [RequireComponent(typeof(Text))]
    public class CardCountUI : MonoBehaviour
    {
        //Assign one of these three to show their card count
        public Player Player;
        public CardStack CardStack;
        public DiscardStack DiscardStack;

        private Text text;

        private void Start()
        {
            text = GetComponent<Text>();
        }

        private void Update()
        {
            int cardCount = 0;

            if (Player != null)
                cardCount = Player.PlayerCardCount;
            else if (CardStack != null)
                cardCount = CardStack.CardCount;
            else if (DiscardStack != null)
                cardCount = DiscardStack.CardCount;
            text.text = cardCount.ToString();
        }

    }

}