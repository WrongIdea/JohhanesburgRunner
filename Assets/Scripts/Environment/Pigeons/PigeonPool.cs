using System.Collections.Generic;
using UnityEngine;

namespace JoburgRunner.Environment.Pigeons
{
    /// <summary>
    /// Fixed pool of pigeons, prewarmed once so no Instantiate/Destroy happens
    /// during gameplay. Idle pigeons are deactivated under a hidden root; the
    /// flock rents and returns them. Grows with a warning only if under-warmed.
    /// </summary>
    public sealed class PigeonPool
    {
        readonly GameObject prefab;
        readonly Transform idleRoot;
        readonly Stack<PigeonController> free = new Stack<PigeonController>();

        public PigeonPool(GameObject prefab, Transform idleRoot, int prewarm)
        {
            this.prefab = prefab;
            this.idleRoot = idleRoot;
            for (int i = 0; i < prewarm; i++)
            {
                free.Push(Create());
            }
        }

        PigeonController Create()
        {
            GameObject go = Object.Instantiate(prefab, idleRoot);
            go.SetActive(false);
            PigeonController pigeon = go.GetComponent<PigeonController>();
            return pigeon;
        }

        public PigeonController Get(Transform parent)
        {
            PigeonController pigeon = free.Count > 0 ? free.Pop() : Create();
            if (pigeon == null)
            {
                return null;
            }
            pigeon.transform.SetParent(parent, false);
            pigeon.gameObject.SetActive(true);
            return pigeon;
        }

        public void Return(PigeonController pigeon)
        {
            if (pigeon == null)
            {
                return;
            }
            pigeon.gameObject.SetActive(false);
            pigeon.transform.SetParent(idleRoot, false);
            free.Push(pigeon);
        }
    }
}
