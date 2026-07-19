using System.Collections.Generic;
using UnityEngine;

namespace JoburgRunner
{
    /// <summary>
    /// Pooled white speed-streak burst played on a perfect dodge. Spawn()
    /// reuses a pooled instance (instantiating only when the pool is empty,
    /// which PerfectDodge pre-warms at scene load), restarts every particle
    /// system, and the instance returns itself to the pool when the burst
    /// ends — nothing is instantiated or destroyed during gameplay.
    /// </summary>
    public class PerfectDodgeVFX : MonoBehaviour
    {
        [SerializeField] float lifeSeconds = 0.4f;

        static readonly Stack<PerfectDodgeVFX> Pool = new Stack<PerfectDodgeVFX>();

        // One additive glow material shared by every streak renderer; the
        // prefab ships with a plain unlit placeholder because the soft glow
        // sprite texture is runtime-generated.
        static Material sharedStreakMaterial;

        ParticleSystem[] systems;
        float returnTime;

        void Awake()
        {
            systems = GetComponentsInChildren<ParticleSystem>(true);
            foreach (ParticleSystem system in systems)
            {
                ParticleSystemRenderer streakRenderer = system.GetComponent<ParticleSystemRenderer>();
                if (streakRenderer == null)
                {
                    continue;
                }

                if (sharedStreakMaterial == null)
                {
                    sharedStreakMaterial = UbuntuPulseVisual.MakeAdditiveGlowMaterial(
                        streakRenderer.sharedMaterial, UbuntuPulseVisual.SoftGlowTexture());
                }

                if (sharedStreakMaterial != null)
                {
                    streakRenderer.sharedMaterial = sharedStreakMaterial;
                }
            }
        }

        /// <summary>
        /// Plays the effect from the pool. Pooled references from a previous
        /// scene arrive destroyed and are skipped (Unity's overloaded null).
        /// </summary>
        public static PerfectDodgeVFX Spawn(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            PerfectDodgeVFX vfx = null;
            while (Pool.Count > 0)
            {
                vfx = Pool.Pop();
                if (vfx != null)
                {
                    break;
                }
            }

            if (vfx == null)
            {
                vfx = Instantiate(prefab).GetComponent<PerfectDodgeVFX>();
            }

            vfx.Play(position, rotation);
            return vfx;
        }

        public void Play(Vector3 position, Quaternion rotation)
        {
            returnTime = Time.time + lifeSeconds;
            transform.SetPositionAndRotation(position, rotation);
            gameObject.SetActive(true);
            for (int i = 0; i < systems.Length; i++)
            {
                systems[i].Clear(true);
                systems[i].Play(true);
            }
        }

        public void ReturnToPool()
        {
            gameObject.SetActive(false);
            Pool.Push(this);
        }

        void Update()
        {
            if (Time.time >= returnTime)
            {
                ReturnToPool();
            }
        }
    }
}
