using UnityEngine;
using UnityEngine.Events;

namespace rummy.Cam
{

    public class CameraAnchor : MonoBehaviour
    {
        public UnityEvent BecameVisible = new();

        private void OnBecameVisible()
        {
            BecameVisible.Invoke();
        }
    }

}