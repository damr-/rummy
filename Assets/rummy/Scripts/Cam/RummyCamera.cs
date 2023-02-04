using UnityEngine;

namespace rummy.Cam
{

    [RequireComponent(typeof(Camera))]
    public class RummyCamera : MonoBehaviour
    {
        public GameMaster GameMaster;
        public int anchorsPerPlayer = 2;
        public float moveSpeed = 15f;
        public float initialSize = 5f;

        private Camera cam;
        private int playersVisible = 0;

        private void Start()
        {
            cam = GetComponent<Camera>();
            Reset();
        }

        private void Update()
        {
            cam.orthographicSize += moveSpeed * Time.deltaTime;
        }

        public void Reset()
        {
            enabled = true;
            cam.orthographicSize = initialSize;
            playersVisible = 0;

            foreach (var p in GameMaster.Players)
            {
                var anchors = p.GetComponentsInChildren<CameraAnchor>();
                foreach (var anchor in anchors)
                    anchor.BecameVisible.AddListener(AnchorBecameVisible);
            }
        }

        private void AnchorBecameVisible()
        {
            playersVisible += 1;
            if (playersVisible == GameMaster.PlayerCount * anchorsPerPlayer)
                enabled = false;
        }
    }

}