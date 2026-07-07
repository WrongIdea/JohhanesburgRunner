using UnityEngine;

namespace JoziGame
{
    public class JoziPickup : MonoBehaviour
    {
        [SerializeField] float rotationSpeed = 105f;

        void Update()
        {
            transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);
        }

        void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player"))
            {
                return;
            }

            JoziGameManager manager = FindAnyObjectByType<JoziGameManager>();
            if (manager != null)
            {
                manager.CollectToken(gameObject);
            }
        }
    }
}
