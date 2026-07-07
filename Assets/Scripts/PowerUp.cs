using UnityEngine;

namespace JoburgRunner
{
    /// <summary>
    /// Floating power-up pickup. Spins and bobs in place; on player contact
    /// it activates its effect through the PowerUpManager.
    /// </summary>
    public class PowerUp : MonoBehaviour
    {
        [SerializeField] PowerUpType type;
        [SerializeField] float rotationSpeed = 90f;
        [SerializeField] float bobAmplitude = 0.18f;
        [SerializeField] float bobFrequency = 1.6f;

        float baseY;

        public PowerUpType Type => type;

        void Start()
        {
            baseY = transform.position.y;
        }

        void Update()
        {
            transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);
            Vector3 position = transform.position;
            position.y = baseY + Mathf.Sin(Time.time * bobFrequency * Mathf.PI) * bobAmplitude;
            transform.position = position;
        }

        void OnTriggerEnter(Collider other)
        {
            PlayerController player = other.GetComponentInParent<PlayerController>();
            if (player == null)
            {
                return;
            }

            PowerUpManager manager = FindAnyObjectByType<PowerUpManager>();
            if (manager != null)
            {
                manager.Activate(type);
            }

            // Deactivate instead of destroy: pickups live inside pooled
            // track chunks and reappear when their chunk is recycled.
            gameObject.SetActive(false);
        }
    }
}
