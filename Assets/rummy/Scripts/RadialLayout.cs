using System.Collections.Generic;
using UnityEngine;

namespace rummy
{

    public abstract class RadialLayout<T> : MonoBehaviour where T : MonoBehaviour
    {
        public float startAngle;
        public float radius = 3f;
        public float angleSpread = 180f;

        public bool leftToRight = false;
        protected float zIncrement = 0;

        public abstract List<T> Objects { get; protected set; }

        private void Start()
        {
            InitValues();
        }

        protected abstract void InitValues();

        public void UpdatePositions()
        {
            if (Objects.Count == 0)
                return;

            // Sign of delta angle depends on the direction
            float deltaAngle = GetDeltaAngle() * (leftToRight ? -1 : 1);
            for (int i = 0; i < Objects.Count; i++)
            {
                // correct for Player rotation
                var angle = startAngle
                    + i * deltaAngle
                    + transform.root.rotation.eulerAngles.z;
                angle *= Mathf.PI / 180f;
                float x = radius * Mathf.Cos(angle);
                float y = radius * Mathf.Sin(angle);
                Objects[i].transform.position = transform.position + new Vector3(x, y, i * zIncrement);
            }
        }

        protected virtual float GetDeltaAngle()
        {
            return angleSpread / Objects.Count;
        }
    }

}