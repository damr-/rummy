using UnityEngine;
using UnityEngine.UI;
using rummy.Utility;
using rummy.Cards;

namespace rummy.UI
{
    [RequireComponent(typeof(Text))]
    public class ValueOutput : MonoBehaviour
    {
        public enum Value
        {
            RoundCount = 0,
            GameSeed = 1,
            PlayerHandCardCount = 3,
            CardStackCardCount = 4,
            DiscardStackCardCount = 5
        }
        public Value OutputValue;

        private Text text;
        private Player Player;
        private CardStack CardStack;
        private DiscardStack DiscardStack;

        private void Start()
        {
            text = GetComponent<Text>();
            if (OutputValue == Value.PlayerHandCardCount)
            {
                Player = GetComponentInParent<Player>();
                if (Player == null)
                    throw new MissingReferenceException("ValueOutput is set to output PlayerHandCardCount but is not child of a Player!");
            }
            else if (OutputValue == Value.CardStackCardCount)
            {
                CardStack = GetComponentInParent<CardStack>();
                if (CardStack == null)
                    throw new MissingReferenceException("ValueOutput is set to output the CardStackCardCount but is not child of a CardStack!");
            }
            else if (OutputValue == Value.DiscardStackCardCount)
            {
                DiscardStack = GetComponentInParent<DiscardStack>();
                if (DiscardStack == null)
                    throw new MissingReferenceException("ValueOutput is set to output the DiscardStackCardCount but is not child of a DiscardStack!");
            }
        }

        private void Update()
        {
            text.text = GetValueOutput();
        }

        private string GetValueOutput()
        {
            switch (OutputValue)
            {
                case Value.RoundCount:
                    return "Round " + Tb.I.GameMaster.RoundCount;
                case Value.GameSeed:
                    return "Seed " + Tb.I.GameMaster.Seed;
                case Value.PlayerHandCardCount:
                    return Player.HandCardCount.ToString();
                case Value.CardStackCardCount:
                    return CardStack.CardCount.ToString();
                default: // case Value.DiscardStackCardCount:
                    return DiscardStack.CardCount.ToString();
            }
        }
    }

}
