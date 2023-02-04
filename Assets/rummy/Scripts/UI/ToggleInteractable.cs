using UnityEngine;
using UnityEngine.UI;

namespace rummy.UI
{
    [RequireComponent(typeof(Slider))]
    public class ToggleInteractable : MonoBehaviour
    {
        private Slider Slider
        {
            get
            {
                if (_s == null)
                    _s = GetComponent<Slider>();
                return _s;
            }
        }
        private Slider _s;

        public void Toggle()
        {
            Slider.interactable = !Slider.IsInteractable();
        }
    }

}