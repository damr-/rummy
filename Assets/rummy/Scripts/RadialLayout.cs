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
        protected float yIncrement = 0;
        protected float yOffset = 0;

        public abstract List<T> Objects { get; protected set; }

        private void Start() { InitValues(); }
        protected abstract void InitValues();

        public void UpdatePositions()
        {
            if (Objects.Count == 0)
                return;

            float deltaAngle = GetDeltaAngle();
            for (int i = 0; i < Objects.Count; i++)
            {
                var angle = startAngle + i * deltaAngle * (leftToRight ? -1 : 1);
                float x = radius * Mathf.Cos(angle * Mathf.PI / 180f);
                float z = radius * Mathf.Sin(angle * Mathf.PI / 180f);
                Objects[i].transform.position = transform.position + new Vector3(x, i * yIncrement + yOffset, z);
            }
        }

        protected virtual float GetDeltaAngle()
        {
            return angleSpread / Objects.Count;
        }
    }

}