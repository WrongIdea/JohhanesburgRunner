using UnityEngine;
using JoburgRunner.Environment.Decor;

namespace JoburgRunner
{
    public class RoadSegmentVisuals : MonoBehaviour
    {
        [SerializeField] GameObject[] districtRoots;
        [SerializeField] bool suppressDistrictBuildings = true;

        public int DistrictCount => districtRoots != null ? districtRoots.Length : 0;

        void Start()
        {
            // The street is assembled from hundreds of primitives; combine them
            // into static batches to collapse draw calls. The segment root can
            // still be moved as a whole when the spawner recycles it.
            //
            // Batch each static category on its own so we can SKIP the runtime
            // decoration container: its props are pooled (re-parented and moved
            // when the tile recycles), and folding them into a static batch would
            // freeze them in place. Sockets carry no renderers, so skip them too.
            foreach (Transform child in transform)
            {
                if (child.name == "DecorRuntime" || child.name == "DecorSockets")
                {
                    continue;
                }

                CombineExcludingPlaceholders(child.gameObject);
            }
        }

        // Static batching freezes combined meshes in place, which would stop the
        // decorator toggling individual placeholder buildings. So any subtree holding
        // a HeroPlaceholderGroup is descended into and combined child-by-child, never
        // folding a placeholder group into a batch. Subtrees without placeholders are
        // combined whole, exactly as before.
        static void CombineExcludingPlaceholders(GameObject root)
        {
            if (root.GetComponentInChildren<HeroPlaceholderGroup>(true) == null)
            {
                StaticBatchingUtility.Combine(root);
                return;
            }

            foreach (Transform child in root.transform)
            {
                if (child.GetComponent<HeroPlaceholderGroup>() != null)
                {
                    continue; // never batch placeholder buildings
                }

                CombineExcludingPlaceholders(child.gameObject);
            }
        }

        public void SetDistrict(int districtIndex)
        {
            if (districtRoots == null || districtRoots.Length == 0)
            {
                return;
            }

            int safeIndex = Mathf.Abs(districtIndex) % districtRoots.Length;
            for (int i = 0; i < districtRoots.Length; i++)
            {
                if (districtRoots[i] != null)
                {
                    districtRoots[i].SetActive(!suppressDistrictBuildings && i == safeIndex);
                }
            }
        }
    }
}
