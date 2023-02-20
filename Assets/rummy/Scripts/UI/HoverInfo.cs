using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace rummy.UI
{

    public class HoverInfo : MonoBehaviour
    {
        public GameObject hoverUI;

        [SerializeField]
        private bool colorizeChildImage = false;
        private int colorIdx = 0;

        private static float alpha = 255 / 255f;
        private static float dark = 200 / 255f;
        public static readonly List<Color> highlightColors = new()
        {
            new( dark,    0, dark, alpha),
            new(    0, dark, dark, alpha),
            new( dark, 0.5f,    0, alpha),
            new(    0, dark,    0, alpha),
            new(    0,    0, dark, alpha)
        };

        public void OnMouseEnter()
        {
            hoverUI.SetActive(true);
            if (colorizeChildImage)
                hoverUI.GetComponentInChildren<Image>().color = highlightColors[colorIdx];
        }

        public void OnMouseExit()
        {
            hoverUI.SetActive(false);
        }

        public void SetHovered(bool hovered, int colorIndex = -1)
        {
            colorIdx = colorIndex > -1 ? colorIndex : 0;
            if (hovered)
                OnMouseEnter();
            else
                OnMouseExit();
        }
    }

}