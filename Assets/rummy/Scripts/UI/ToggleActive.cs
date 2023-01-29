using UnityEngine;

namespace rummy.UI
{

    public class ToggleActive : MonoBehaviour
    {
        public void Toggle()
        {
            gameObject.SetActive(!gameObject.activeSelf);
        }
    }

}