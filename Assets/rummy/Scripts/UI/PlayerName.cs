using TMPro;
using UnityEngine;

namespace rummy.UI
{

    [RequireComponent(typeof(TextMeshProUGUI))]
    public class PlayerName : MonoBehaviour
    {
        private TextMeshProUGUI _playerName;
        private TextMeshProUGUI Name
        {
            get {
                if (_playerName == null)
                    _playerName = GetComponent<TextMeshProUGUI>();
                return _playerName;
            }
        }

        public void UpdateName(string newName)
        {
            Name.text = newName;
        }

        public void EnableUnderline(bool enable)
        {
            Name.fontStyle = enable ? FontStyles.Underline : FontStyles.Normal;
        }
    }

}