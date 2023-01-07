using UnityEngine;

namespace rummy.Cam
{

    [RequireComponent(typeof(Camera))]
    public class RummyCamera : MonoBehaviour
    {
        public float moveSpeed = 15f;
        public float initialSize = 5f;
        private Camera cam;

        private int playerCount = 0;
        private int playersVisible = 0;

        private void Start()
        {
            cam = GetComponent<Camera>();
            cam.orthographicSize = initialSize;

            var players = FindObjectsOfType<Player>();
            playerCount = players.Length;
            foreach(var p in players)
            {
                p.GetComponentInChildren<CameraAnchor>().BecameVisible.AddListener(PlayerBecameVisible);
            }
        }

        private void Update()
        {
            cam.orthographicSize += moveSpeed * Time.deltaTime;
        }

        private void PlayerBecameVisible()
        {
            playersVisible += 1;
            if (playersVisible >= playerCount)
            {
                enabled = false;
            }
        }
    }

}