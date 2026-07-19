using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace JoburgRunner
{
    /// <summary>
    /// Visual-only blue dissolve for small obstacles absorbed by Ubuntu Pulse.
    /// Gameplay still decides when this is called. This component only applies
    /// a temporary MaterialPropertyBlock glow, reuses a small pooled VFX prefab,
    /// clears the property block, then deactivates the obstacle for the existing
    /// chunk pooling flow.
    /// </summary>
    public class ObstacleDissolveEffect : MonoBehaviour
    {
        [SerializeField] float dissolveSeconds = 0.4f;
        [SerializeField] Color dissolveTint = new Color(0.45f, 0.85f, 1f, 1f);
        [SerializeField] float dissolveGlowIntensity = 2.2f;
        [SerializeField, Range(8, 96)] int particleCount = 36;
        [SerializeField, Range(0f, 1f)] float dissolveEndScale = 0.05f;
        [SerializeField] GameObject dissolveParticlePrefab;

        struct RendererState
        {
            public Renderer Renderer;
            public MaterialPropertyBlock Block;
        }

        static readonly Dictionary<GameObject, Queue<GameObject>> Pools = new Dictionary<GameObject, Queue<GameObject>>();

        RendererState[] states;
        Coroutine active;

        void Awake()
        {
            Renderer[] renderers = GetComponentsInChildren<Renderer>();
            states = new RendererState[renderers.Length];
            for (int i = 0; i < renderers.Length; i++)
            {
                states[i] = new RendererState
                {
                    Renderer = renderers[i],
                    Block = new MaterialPropertyBlock(),
                };
            }
        }

        public void Dissolve()
        {
            if (active != null)
            {
                StopCoroutine(active);
            }

            PlayPooledDissolvePrefab();
            active = StartCoroutine(FadeAndDeactivate());
        }

        IEnumerator FadeAndDeactivate()
        {
            // Obstacles use opaque materials, so alpha alone cannot fade
            // them out — shrinking toward zero is what actually reads as
            // "dissolving"; the blue tint and particle burst sell the energy.
            Vector3 startScale = transform.localScale;
            float elapsed = 0f;
            while (elapsed < dissolveSeconds)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / dissolveSeconds);
                float alpha = 1f - progress;
                transform.localScale = startScale * Mathf.Lerp(1f, dissolveEndScale, progress * progress);
                foreach (RendererState state in states)
                {
                    Color color = dissolveTint * (1f + dissolveGlowIntensity * 0.2f * alpha);
                    color.a = alpha;
                    state.Renderer.GetPropertyBlock(state.Block);
                    state.Block.SetColor("_BaseColor", color);
                    state.Block.SetColor("_Color", color);
                    state.Renderer.SetPropertyBlock(state.Block);
                }

                yield return null;
            }

            foreach (RendererState state in states)
            {
                state.Renderer.SetPropertyBlock(null);
            }

            // Restore the rest scale while still invisible so the pooled
            // obstacle respawns at full size.
            transform.localScale = startScale;
            active = null;
            gameObject.SetActive(false);
        }

        void PlayPooledDissolvePrefab()
        {
            if (dissolveParticlePrefab == null)
            {
                return;
            }

            GameObject instance = GetPooled(dissolveParticlePrefab);
            instance.transform.position = transform.position + Vector3.up * 0.45f;
            instance.transform.rotation = Quaternion.identity;
            instance.SetActive(true);

            foreach (ParticleSystem particles in instance.GetComponentsInChildren<ParticleSystem>(true))
            {
                ParticleSystem.MainModule main = particles.main;
                main.maxParticles = Mathf.Max(main.maxParticles, particleCount);
                particles.Clear(true);
                particles.Play(true);
            }

            StartCoroutine(ReturnWhenFinished(dissolveParticlePrefab, instance));
        }

        static GameObject GetPooled(GameObject prefab)
        {
            if (!Pools.TryGetValue(prefab, out Queue<GameObject> pool))
            {
                pool = new Queue<GameObject>();
                Pools[prefab] = pool;
            }

            while (pool.Count > 0)
            {
                GameObject item = pool.Dequeue();
                if (item != null)
                {
                    return item;
                }
            }

            GameObject created = Instantiate(prefab);
            created.name = $"{prefab.name}_Pooled";
            UpgradeParticleGlow(created);
            return created;
        }

        // The dissolve prefab ships with an opaque unlit material that draws
        // particles as hard squares; swap in the shared soft additive glow so
        // they read as blue energy motes.
        static void UpgradeParticleGlow(GameObject instance)
        {
            foreach (ParticleSystemRenderer renderer in instance.GetComponentsInChildren<ParticleSystemRenderer>(true))
            {
                Material glow = UbuntuPulseVisual.MakeAdditiveGlowMaterial(renderer.sharedMaterial, UbuntuPulseVisual.SoftGlowTexture());
                if (glow != null)
                {
                    renderer.sharedMaterial = glow;
                }
            }
        }

        static IEnumerator ReturnWhenFinished(GameObject prefab, GameObject instance)
        {
            yield return new WaitForSeconds(0.75f);
            if (instance == null)
            {
                yield break;
            }

            instance.SetActive(false);
            if (!Pools.TryGetValue(prefab, out Queue<GameObject> pool))
            {
                pool = new Queue<GameObject>();
                Pools[prefab] = pool;
            }

            pool.Enqueue(instance);
        }
    }
}
