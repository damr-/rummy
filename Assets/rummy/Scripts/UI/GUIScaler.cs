using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace rummy.UI
{

    public class GUIScaler : MonoBehaviour
    {
        public float GUIScale = 1.0f;
        public float deltaGUIScale = 0.25f;
        public float minScale = 0.5f;
        public float maxScale = 5f;

        // Use parents for easier identification of the objects in the editor
        public Transform[] canvasScalerParents;
        private readonly List<CanvasScaler> canvasScalers = new();
        
        private void Start()
        {
            foreach (var t in canvasScalerParents)
                canvasScalers.Add(t.GetComponent<CanvasScaler>());
        }

        public void AddCanvasScaler(CanvasScaler scaler)
        {
             canvasScalers.Add(scaler);
             UpdateCanvasScalers();
        }

        public void ChangeScale(bool increase)
        {
            GUIScale += deltaGUIScale * (increase ? 1 : -1);
            GUIScale = Mathf.Min(Mathf.Max(GUIScale, minScale), maxScale);
            UpdateCanvasScalers();
        }

        private void UpdateCanvasScalers()
        {
            foreach (var scaler in canvasScalers)
                scaler.scaleFactor = GUIScale;
        }
    }

}