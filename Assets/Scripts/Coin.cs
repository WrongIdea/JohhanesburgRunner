using System.Collections.Generic;
using UnityEngine;

namespace JoburgRunner
{
    /// <summary>
    /// Rotating collectible coin. The common R1 is worth one coin; the rare
    /// R5 variant is worth five and is tracked as a collectable.
    /// Set the collider to Is Trigger and make sure the Player has PlayerController.
    /// </summary>
    public class Coin : MonoBehaviour
    {
        public static readonly List<Coin> ActiveCoins = new List<Coin>();

        [SerializeField] float rotationSpeed = 120f;
        [SerializeField] int coinValue = 1;
        [SerializeField] bool isRare;
        [SerializeField] GameObject collectParticlePrefab;
        [SerializeField] ScoreManager scoreManager;

        void OnEnable()
        {
            ActiveCoins.Add(this);
        }

        void OnDisable()
        {
            ActiveCoins.Remove(this);
        }

        void Update()
        {
            transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);
        }

        void OnTriggerEnter(Collider other)
        {
            PlayerController player = other.GetComponentInParent<PlayerController>();
            if (player == null)
            {
                return;
            }

            if (collectParticlePrefab != null)
            {
                Instantiate(collectParticlePrefab, transform.position, Quaternion.identity);
            }

            if (scoreManager == null)
            {
                scoreManager = FindAnyObjectByType<ScoreManager>();
            }

            if (scoreManager != null)
            {
                scoreManager.AddCoins(coinValue, isRare);
            }

            // Deactivate instead of destroy: coins live inside pooled track
            // chunks and reappear when their chunk is recycled.
            gameObject.SetActive(false);
        }
    }
}
