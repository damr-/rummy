using UnityEngine;

namespace rummy.Cam
{

    [RequireComponent(typeof(Camera))]
    public class RummyCamera : MonoBehaviour
    {
        private readonly float adjustSpeed = 5f;
        private Camera cam;

        private void Start()
        {
            cam = GetComponent<Camera>();
        }

        private void Update()
        {
            cam.orthographicSize += adjustSpeed * Time.deltaTime;
        }

        public void AnchorBecameVisible()
        {
            enabled = false;
        }
    }

}