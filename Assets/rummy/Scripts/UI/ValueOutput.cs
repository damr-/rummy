using UnityEngine;
using TMPro;
using rummy.Utility;
using rummy.Cards;

namespace rummy.UI
{
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class ValueOutput : MonoBehaviour
    {
        public enum Value
        {
            RoundCount = 0,
            Seed = 1,
            PlayerState = 2,
            PlayerHandCardCount = 3,
            CardStackCardCount = 4,
            DiscardStackCardCount = 5,
            MinLaySum = 6,
            GuiScale = 7,
            GameSpeed = 8,
            PlayWaitDuration = 9,
            DrawWaitDuration = 10,
            PlayerCount = 11,
            PlayCardMoveSpeed = 12,
            DealCardMoveSpeed = 13,
        }
        public Value OutputValue;

        private TextMeshProUGUI text;
        private Player Player;
        private CardStack CardStack;
        private DiscardStack DiscardStack;
        private GameMaster GameMaster;
        private GUIScaler GUIScaler;

        private void Start()
        {
            text = GetComponent<TextMeshProUGUI>();
            switch (OutputValue)
            {
                case Value.PlayerState:
                case Value.PlayerHandCardCount:
                    Player = GetComponentInParent<Player>();
                    if (Player == null)
                        throw new MissingReferenceException($"ValueOutput is set to output {OutputValue} but is not child of a Player");
                    break;
                case Value.CardStackCardCount:
                    CardStack = GetComponentInParent<CardStack>();
                    if (CardStack == null)
                        throw new MissingReferenceException($"ValueOutput is set to output {OutputValue} but is not child of a CardStack");
                    break;
                case Value.DiscardStackCardCount:
                    DiscardStack = GetComponentInParent<DiscardStack>();
                    if (DiscardStack == null)
                        throw new MissingReferenceException($"ValueOutput is set to output {OutputValue} but is not child of a DiscardStack");
                    break;
                case Value.RoundCount:
                case Value.Seed:
                case Value.MinLaySum:
                case Value.GameSpeed:
                case Value.PlayWaitDuration:
                case Value.DrawWaitDuration:
                case Value.PlayerCount:
                case Value.PlayCardMoveSpeed:
                case Value.DealCardMoveSpeed:
                    GameMaster = GetComponentInParent<GameMaster>();
                    if (GameMaster == null)
                        throw new MissingReferenceException($"ValueOutput is set to output {OutputValue} but is not child of a GameMaster");
                    break;
                case Value.GuiScale:
                    GUIScaler = GetComponentInParent<GUIScaler>();
                    if (GUIScaler == null)
                        throw new MissingReferenceException($"ValueOutput is set to output {OutputValue} but is not child of a GUIScaler");
                    break;
            }
        }

        private void Update()
        {
            if (isActiveAndEnabled)
            {
                text.text = OutputValue switch
                {
                    Value.RoundCount => GameMaster.RoundCount.ToString(),
                    Value.Seed => GameMaster.Seed.ToString(),
                    Value.PlayerState => Player.State.ToString().FirstCharsToUpper(),
                    Value.PlayerHandCardCount => Player.HandCardCount.ToString(),
                    Value.CardStackCardCount => CardStack.CardCount.ToString(),
                    Value.DiscardStackCardCount => DiscardStack.CardCount.ToString(),
                    Value.MinLaySum => GameMaster.MinimumLaySum.ToString(),
                    Value.GuiScale => GUIScaler.GUIScale.ToString("0.00"),
                    Value.GameSpeed => GameMaster.GameSpeed.ToString("0"),
                    Value.PlayWaitDuration => GameMaster.PlayWaitDuration.ToString("0.00"),
                    Value.DrawWaitDuration => GameMaster.DrawWaitDuration.ToString("0.00"),
                    Value.PlayerCount => GameMaster.PlayerCount.ToString(),
                    Value.PlayCardMoveSpeed => GameMaster.PlayCardMoveSpeed.ToString("0"),
                    Value.DealCardMoveSpeed => GameMaster.DealCardMoveSpeed.ToString("0"),
                    _ => throw new RummyException("Invalid output value type: " + OutputValue)
                };
            }
        }
    }

}
