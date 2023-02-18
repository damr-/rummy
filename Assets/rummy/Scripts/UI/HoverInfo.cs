using UnityEngine;

namespace rummy.UI
{

    public class HoverInfo : MonoBehaviour
    {
        public GameObject hoverUI;

        public void OnMouseEnter()
        {
            hoverUI.SetActive(true);
        }

        public void OnMouseExit()
        {
            hoverUI.SetActive(false);
        }

        public void SetHovered(bool hovered)
        {
            if (hovered)
                OnMouseEnter();
            else
                OnMouseExit();
        }
    }

}