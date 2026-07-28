using System.Collections.Generic;
using UnityEngine;

namespace JoburgRunner.Environment.Decor
{
    /// <summary>
    /// Marks a non-static-batched root holding the placeholder buildings on one side
    /// of a CBD tile. When a hero building occupies that side's socket, only the
    /// placeholders its footprint overlaps are deactivated; they are restored when the
    /// hero is released (tile recycled or re-dressed to another district).
    ///
    /// Deactivation uses <see cref="GameObject.SetActive"/> (not Renderer.enabled) so
    /// it survives the district-level cull LODGroup, which drives Renderer.enabled and
    /// would otherwise re-show a hidden placeholder. This root is excluded from static
    /// batching by <see cref="RoadSegmentVisuals"/> so SetActive stays authoritative.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HeroPlaceholderGroup : MonoBehaviour
    {
        readonly List<GameObject> hidden = new List<GameObject>();

        public bool HasHidden => hidden.Count > 0;

        /// <summary>
        /// Hides each direct-child placeholder whose combined renderer bounds fall
        /// within <paramref name="radius"/> of <paramref name="worldCenter"/>. Records
        /// exactly what it hid so <see cref="RestoreAll"/> can undo it.
        /// </summary>
        public void Hide(Vector3 worldCenter, float radius)
        {
            float r2 = radius * radius;
            foreach (Transform child in transform)
            {
                GameObject go = child.gameObject;
                if (!go.activeSelf)
                {
                    continue;
                }

                if (Overlaps(child, worldCenter, r2))
                {
                    go.SetActive(false);
                    hidden.Add(go);
                }
            }
        }

        /// <summary>Restores every placeholder this group hid, back to full default state.</summary>
        public void RestoreAll()
        {
            for (int i = 0; i < hidden.Count; i++)
            {
                if (hidden[i] != null)
                {
                    hidden[i].SetActive(true);
                }
            }

            hidden.Clear();
        }

        static bool Overlaps(Transform unit, Vector3 center, float r2)
        {
            var renderers = unit.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                return false;
            }

            Bounds b = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                b.Encapsulate(renderers[i].bounds);
            }

            return (b.ClosestPoint(center) - center).sqrMagnitude <= r2;
        }
    }
}
