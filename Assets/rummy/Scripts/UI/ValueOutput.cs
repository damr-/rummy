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
            DiscardStackCardCount = 5,
            MinLaySum = 6,
        }
        public Value OutputValue;

        private TextMeshProUGUI text;
        private Player Player;
        private CardStack CardStack;
        private DiscardStack DiscardStack;
        private GameMaster GameMaster;

        private void Start()
        {
            text = GetComponent<TextMeshProUGUI>();
            switch (OutputValue)
            {
                case Value.PlayerHandCardCount:
                    Player = GetComponentInParent<Player>();
                    if (Player == null)
                        throw new MissingReferenceException("ValueOutput is set to output PlayerHandCardCount but is not child of a Player!");
                    break;
                case Value.CardStackCardCount:
                    CardStack = GetComponentInParent<CardStack>();
                    if (CardStack == null)
                        throw new MissingReferenceException("ValueOutput is set to output the CardStackCardCount but is not child of a CardStack!");
                    break;
                case Value.DiscardStackCardCount:
                    DiscardStack = GetComponentInParent<DiscardStack>();
                    if (DiscardStack == null)
                        throw new MissingReferenceException("ValueOutput is set to output the DiscardStackCardCount but is not child of a DiscardStack!");
                    break;
                case Value.MinLaySum:
                    GameMaster = GetComponentInParent<GameMaster>();
                    if (GameMaster == null)
                        throw new MissingReferenceException("ValueOutput is set to output the MinLaySum but is not child of the GameMaster!");
                    break;
            }
        }

        private void Update()
        {
            text.text = OutputValue switch
            {
                Value.RoundCount => "Round " + Tb.I.GameMaster.RoundCount,
                Value.GameSeed => "Game Seed: " + Tb.I.GameMaster.Seed,
                Value.PlayerHandCardCount => Player.HandCardCount.ToString(),
                Value.CardStackCardCount => CardStack.CardCount.ToString(),
                Value.DiscardStackCardCount => DiscardStack.CardCount.ToString(),
                Value.MinLaySum => GameMaster.MinimumLaySum.ToString(),
                _ => throw new RummyException("Invalid output value type: " + OutputValue)
            };
        }
    }

}
