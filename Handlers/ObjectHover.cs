using UnityEngine;

namespace _project.Scripts.Handlers
{
    public class ObjectHover : MonoBehaviour
    {
        [Header("Hover Settings")]
        public bool hover = true;
        public float hoverHeight = 0.5f, hoverSpeed = 2.0f;

        [Header("Rotation Settings")] 
        public bool rotation = true;
        public float rotationSpeed = 15.0f;

        private Vector3 _initialPosition;

        private void Start() => _initialPosition = transform.position;

        private void Update()
        {
            if (hover)
                transform.position = _initialPosition + Vector3.up * (Mathf.Sin(Time.time * hoverSpeed) * hoverHeight);
            if (rotation)
                transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);
        }
    }
}