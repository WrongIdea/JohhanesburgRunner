using UnityEngine;

namespace JoburgRunner
{
    public enum JunctionType
    {
        None,
        Crossroad,
        TLeft,
        TRight
    }

    /// <summary>Switches purely visual side-road geometry on a pooled road tile.</summary>
    public sealed class JunctionVisuals : MonoBehaviour
    {
        [SerializeField] GameObject crossroadRoot;
        [SerializeField] GameObject tLeftRoot;
        [SerializeField] GameObject tRightRoot;

        public JunctionType ActiveType { get; private set; }

        public void SetType(JunctionType type)
        {
            ActiveType = type;
            if (crossroadRoot != null) crossroadRoot.SetActive(type == JunctionType.Crossroad);
            if (tLeftRoot != null) tLeftRoot.SetActive(type == JunctionType.TLeft);
            if (tRightRoot != null) tRightRoot.SetActive(type == JunctionType.TRight);
        }

#if UNITY_EDITOR
        void OnDrawGizmosSelected()
        {
            Matrix4x4 old = Gizmos.matrix;
            Gizmos.matrix = transform.localToWorldMatrix;

            // Three unchanged gameplay lanes and the overall playable road bounds.
            Gizmos.color = new Color(0f, 1f, 0.25f, 0.8f);
            foreach (float x in new[] { -RoadMetrics.LaneSpacing, 0f, RoadMetrics.LaneSpacing })
            {
                Gizmos.DrawWireCube(new Vector3(x, 0.5f, 15f), new Vector3(2.5f, 1f, 30f));
            }

            Gizmos.color = Color.white;
            Gizmos.DrawLine(new Vector3(-RoadMetrics.RoadHalfWidth, 0f, 0f), new Vector3(RoadMetrics.RoadHalfWidth, 0f, 0f));
            Gizmos.DrawLine(new Vector3(-RoadMetrics.RoadHalfWidth, 0f, 30f), new Vector3(RoadMetrics.RoadHalfWidth, 0f, 30f));

            Gizmos.color = new Color(0.2f, 0.65f, 1f, 0.75f);
            Gizmos.DrawWireCube(new Vector3(-19.625f, 0f, 7f), new Vector3(30.75f, 0.4f, 12f));
            Gizmos.DrawWireCube(new Vector3(19.625f, 0f, 7f), new Vector3(30.75f, 0.4f, 12f));
            Gizmos.matrix = old;
        }
#endif
    }
}
