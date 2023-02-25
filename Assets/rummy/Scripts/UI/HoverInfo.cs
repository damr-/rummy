using rummy.UI.CardOutput;
using UnityEngine;
using UnityEngine.UI;

namespace rummy.UI
{

    public class HoverInfo : MonoBehaviour
    {
        public GameObject hoverUI;

        # region ColoredCardHighlight
        [SerializeField]
        private bool colorizeChildImage = false;
        private Color highlightColor = CardCombosUI.highlightColors[0];
        private Image _childImage = null;
        private Image ChildImage 
        {
            get 
            {
                if (_childImage == null)
                    _childImage = hoverUI.GetComponentInChildren<Image>();
                return _childImage;
            }
        }
        #endregion

        public void OnMouseEnter()
        {
            hoverUI.SetActive(true);
            if (colorizeChildImage)
                ChildImage.color = highlightColor;
        }

        public void OnMouseExit()
        {
            hoverUI.SetActive(false);
        }

        public void SetHovered(bool hovered)
        {
            SetHovered(hovered, CardCombosUI.highlightColors[0]);
        }

        public void SetHovered(bool hovered, Color childImageColor)
        {
            highlightColor = childImageColor;
            if (hovered)
                OnMouseEnter();
            else
                OnMouseExit();
        }
    }

}