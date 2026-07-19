using System.Collections.Generic;
using UnityEngine;

namespace JoburgRunner.VFX
{
    /// <summary>
    /// One pool of instances for a single <see cref="VFXDefinition"/>. Instances
    /// are recycled via SetActive (never destroyed during play), auto-returning
    /// after the definition's lifetime. Respects the definition's instance cap:
    /// once every instance is live, further spawns are dropped rather than
    /// allocating, which keeps coin/step bursts allocation-free under load.
    /// </summary>
    public sealed class VFXPool
    {
        readonly VFXDefinition definition;
        readonly Transform poolRoot;
        readonly Stack<Transform> idle = new Stack<Transform>();
        readonly List<Live> live = new List<Live>();
        int created;

        struct Live
        {
            public Transform instance;
            public float returnTime;
        }

        public VFXPool(VFXDefinition definition, Transform poolRoot)
        {
            this.definition = definition;
            this.poolRoot = poolRoot;

            for (int i = 0; i < definition.prewarm && definition.prefab != null; i++)
            {
                Transform instance = CreateInstance();
                instance.gameObject.SetActive(false);
                idle.Push(instance);
            }
        }

        public void Play(in GameplayVFXContext context)
        {
            if (definition.prefab == null)
            {
                return;
            }

            if (definition.maxInstances > 0 && live.Count >= definition.maxInstances)
            {
                return; // at cap — drop rather than allocate
            }

            Transform instance = idle.Count > 0 ? idle.Pop() : CreateInstance();
            if (instance == null)
            {
                return;
            }

            Transform parent = definition.followTarget && context.follow != null ? context.follow : poolRoot;
            instance.SetParent(parent, false);
            instance.position = context.position + (parent != null ? parent.TransformVector(definition.spawnOffset) : definition.spawnOffset);
            instance.localScale = Vector3.one * definition.scale;
            instance.gameObject.SetActive(true);

            RestartParticles(instance);

            live.Add(new Live { instance = instance, returnTime = Time.time + definition.lifetime });
        }

        /// <summary>Returns finished instances to the pool. Call once per frame.</summary>
        public void Tick()
        {
            for (int i = live.Count - 1; i >= 0; i--)
            {
                if (Time.time < live[i].returnTime)
                {
                    continue;
                }

                Transform instance = live[i].instance;
                live.RemoveAt(i);
                if (instance == null)
                {
                    continue;
                }

                instance.gameObject.SetActive(false);
                instance.SetParent(poolRoot, false);
                idle.Push(instance);
            }
        }

        /// <summary>Recalls every live instance immediately (crash/restart/scene change).</summary>
        public void Clear()
        {
            foreach (Live entry in live)
            {
                if (entry.instance != null)
                {
                    entry.instance.gameObject.SetActive(false);
                    entry.instance.SetParent(poolRoot, false);
                    idle.Push(entry.instance);
                }
            }

            live.Clear();
        }

        Transform CreateInstance()
        {
            created++;
            GameObject go = Object.Instantiate(definition.prefab, poolRoot);
            go.name = $"{definition.name}_{created}";
            return go.transform;
        }

        static void RestartParticles(Transform instance)
        {
            ParticleSystem[] systems = instance.GetComponentsInChildren<ParticleSystem>(true);
            foreach (ParticleSystem system in systems)
            {
                system.Clear(true);
                system.Play(true);
            }

            TrailRenderer[] trails = instance.GetComponentsInChildren<TrailRenderer>(true);
            foreach (TrailRenderer trail in trails)
            {
                trail.Clear();
            }
        }
    }
}
