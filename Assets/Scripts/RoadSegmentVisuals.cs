using UnityEngine;

namespace JoburgRunner
{
    public class RoadSegmentVisuals : MonoBehaviour
    {
        [SerializeField] GameObject[] districtRoots;

        public int DistrictCount => districtRoots != null ? districtRoots.Length : 0;

        void Start()
        {
            // The street is assembled from hundreds of primitives; combine them
            // into static batches to collapse draw calls. The segment root can
            // still be moved as a whole when the spawner recycles it.
            StaticBatchingUtility.Combine(gameObject);
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
                    districtRoots[i].SetActive(i == safeIndex);
                }
            }
        }
    }
}
