using System.Collections.Generic;
using rummy.UI.Options;
using UnityEngine;
using UnityEngine.UI;

namespace rummy.UI
{

    public class GUIScaler : MonoBehaviour
    {
        public GUIScaleUI guiScaleUI;

        public float GUIScale = 1.0f;
        public float deltaGUIScale = 0.25f;
        public float minScale = 0.5f;
        public float maxScale = 5f;

        // Use parents for easier identification of the objects in the editor
        public Transform[] canvasScalerParents;
        private readonly List<CanvasScaler> canvasScalers = new List<CanvasScaler>();

        private void Start()
        {
            foreach (var t in canvasScalerParents)
                canvasScalers.Add(t.GetComponent<CanvasScaler>());
        }

        public void ChangeScale(bool increase)
        {
            GUIScale = Mathf.Max(GUIScale, minScale);
            GUIScale = Mathf.Min(GUIScale, maxScale);

            GUIScale += deltaGUIScale * (increase ? 1 : -1);
            foreach (var scaler in canvasScalers)
                scaler.scaleFactor = GUIScale;

            guiScaleUI.GUIScale = GUIScale;
        }
    }

}