using UnityEngine;
using UnityEngine.UI;

namespace rummy.UI
{
    [RequireComponent(typeof(Button))]
    public class EndTurnButton : MonoBehaviour
    {
        private Player _player = null;
        private Player Player
        {
            get
            {
                if (_player == null)
                    _player = GetComponentInParent<Player>();
                return _player;
            }
        }
        private Button _button = null;
        private Button Button
        {
            get
            {
                if (_button == null)
                    _button = GetComponentInParent<Button>();
                return _button;
            }
        }

        private void Start()
        {
            Player.StateChanged.AddListener(state =>
                Button.interactable = state == Player.PlayerState.PLAYING || state == Player.PlayerState.DISCARDING);
        }
    }

}