using System.Collections.Generic;
using UnityEngine;

namespace JoburgRunner.Environment.Decor
{
    /// <summary>
    /// Prefab-keyed object pool for decoration props. Warmed once at startup so
    /// no <see cref="Object.Instantiate"/> / <see cref="Object.Destroy"/> happens
    /// during play – recycled tiles just re-parent existing instances. Idle
    /// instances are deactivated and held under a single hidden root.
    /// </summary>
    public sealed class DecorPool
    {
        readonly Dictionary<GameObject, Stack<GameObject>> free = new Dictionary<GameObject, Stack<GameObject>>();
        readonly Dictionary<GameObject, GameObject> prefabOf = new Dictionary<GameObject, GameObject>();
        readonly Transform idleRoot;

        public DecorPool(Transform idleRoot)
        {
            this.idleRoot = idleRoot;
        }

        /// <summary>Pre-instantiates <paramref name="count"/> copies of a prefab.</summary>
        public void Prewarm(GameObject prefab, int count)
        {
            if (prefab == null || count <= 0)
            {
                return;
            }

            if (!free.TryGetValue(prefab, out Stack<GameObject> stack))
            {
                stack = new Stack<GameObject>(count);
                free[prefab] = stack;
            }

            for (int i = 0; i < count; i++)
            {
                stack.Push(CreateInstance(prefab));
            }
        }

        GameObject CreateInstance(GameObject prefab)
        {
            GameObject instance = Object.Instantiate(prefab, idleRoot);
            instance.SetActive(false);
            prefabOf[instance] = prefab;
            return instance;
        }

        /// <summary>
        /// Rents an instance of <paramref name="prefab"/>, parenting it to
        /// <paramref name="parent"/>. Grows the pool (with a warning) only if it
        /// was under-warmed, so warm-up sizing can be corrected without crashing.
        /// </summary>
        public GameObject Get(GameObject prefab, Transform parent)
        {
            GameObject instance = GetInactive(prefab, parent);
            if (instance != null)
            {
                instance.SetActive(true);
            }
            return instance;
        }

        /// <summary>Rent inactive so callers can finish placement before rendering.</summary>
        public GameObject GetInactive(GameObject prefab, Transform parent)
        {
            if (prefab == null)
            {
                return null;
            }

            if (!free.TryGetValue(prefab, out Stack<GameObject> stack))
            {
                stack = new Stack<GameObject>();
                free[prefab] = stack;
            }

            GameObject instance;
            if (stack.Count > 0)
            {
                instance = stack.Pop();
            }
            else
            {
                Debug.LogWarning($"DecorPool under-warmed for '{prefab.name}'; growing at runtime. " +
                                 "Increase its prewarm count to avoid a hitch.");
                instance = CreateInstance(prefab);
            }

            instance.transform.SetParent(parent, false);
            return instance;
        }

        /// <summary>Returns an instance to its pool and deactivates it.</summary>
        public void Release(GameObject instance)
        {
            if (instance == null)
            {
                return;
            }

            if (!prefabOf.TryGetValue(instance, out GameObject prefab))
            {
                // Not ours – just disable it so it stops rendering.
                instance.SetActive(false);
                return;
            }

            instance.SetActive(false);
            instance.transform.SetParent(idleRoot, false);
            free[prefab].Push(instance);
        }
    }
}
