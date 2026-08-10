using UnityEngine;

namespace JoburgRunner
{
    /// <summary>
    /// Selects one preloaded taxi body whenever a pooled taxi obstacle is reset.
    /// The collider and driving behaviour stay shared, so variety does not alter
    /// lane placement, difficulty, or obstacle spawning.
    /// </summary>
    public class TaxiObstacleVisualVariants : MonoBehaviour
    {
        [SerializeField] GameObject[] variants;
        int previous = -1;

        public void ChooseVariant()
        {
            if (variants == null || variants.Length == 0)
            {
                return;
            }

            int chosen = Random.Range(0, variants.Length);
            if (variants.Length > 1 && chosen == previous)
            {
                chosen = (chosen + 1 + Random.Range(0, variants.Length - 1)) % variants.Length;
            }

            for (int index = 0; index < variants.Length; index++)
            {
                if (variants[index] != null)
                {
                    variants[index].SetActive(index == chosen);
                }
            }

            previous = chosen;
        }
    }
}
