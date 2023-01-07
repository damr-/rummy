using UnityEngine;
using rummy.Utility;
using rummy.Cards;
using TMPro;

namespace rummy.UI
{
    [RequireComponent(typeof(TextMeshProUGUI))]
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

        private TextMeshProUGUI text;
        private Player Player;
        private CardStack CardStack;
        private DiscardStack DiscardStack;

        private void Start()
        {
            text = GetComponent<TextMeshProUGUI>();
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
            text.text = OutputValue switch
            {
                Value.RoundCount => "Round " + Tb.I.GameMaster.RoundCount,
                Value.GameSeed => "Seed " + Tb.I.GameMaster.Seed,
                Value.PlayerHandCardCount => Player.HandCardCount.ToString(),
                Value.CardStackCardCount => CardStack.CardCount.ToString(),
                Value.DiscardStackCardCount => DiscardStack.CardCount.ToString(),
                _ => throw new RummyException("Invalid output value type: " + OutputValue)
            };
        }
    }

}
