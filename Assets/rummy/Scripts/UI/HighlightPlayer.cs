using UnityEngine;
using UnityEngine.UI;

namespace rummy.UI
{

    [RequireComponent(typeof(Image))]
    public class HighlightPlayer : MonoBehaviour
    {
        private Image _highlight;
        private Image Highlight
        {
            get
            {
                if (_highlight == null)
                    _highlight = GetComponent<Image>();
                return _highlight;
            }
        }

        public void EnableHighlight(bool enabled)
        {
            Highlight.enabled = enabled;
        }
    }

}