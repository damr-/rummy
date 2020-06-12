using UnityEngine;

namespace rummy.Cam
{

    [RequireComponent(typeof(Camera))]
    public class RummyCamera : MonoBehaviour
    {
        public float moveSpeed = 15f;
        public float initialSize = 5f;
        private Camera cam;

        private void Start()
        {
            cam = GetComponent<Camera>();
            cam.orthographicSize = initialSize;
        }

        private void Update()
        {
            cam.orthographicSize += moveSpeed * Time.deltaTime;
        }

        public void AnchorBecameVisible()
        {
            enabled = false;
        }
    }

}