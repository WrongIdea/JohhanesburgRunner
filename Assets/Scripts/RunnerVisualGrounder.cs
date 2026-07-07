using UnityEngine;

namespace JoburgRunner
{
    /// <summary>
    /// Keeps the oversized runner visual planted on the road even when the FBX
    /// animation shifts its rendered bounds vertically.
    /// </summary>
    public class RunnerVisualGrounder : MonoBehaviour
    {
        [SerializeField] Transform visualRoot;
        [SerializeField] Transform groundRoot;
        [SerializeField] float groundSink = 0.35f;
        [SerializeField] bool centerOnLane = true;

        Renderer[] renderers;
        PlayerDeathVisual deathVisual;

        void Awake()
        {
            if (visualRoot == null)
            {
                visualRoot = transform;
            }

            if (groundRoot == null)
            {
                groundRoot = transform.parent != null ? transform.parent : transform;
            }

            renderers = visualRoot.GetComponentsInChildren<Renderer>(true);
            deathVisual = GetComponent<PlayerDeathVisual>();
        }

        void LateUpdate()
        {
            if (visualRoot == null || groundRoot == null || renderers == null || renderers.Length == 0)
            {
                return;
            }

            if (deathVisual != null && deathVisual.IsDead)
            {
                return;
            }

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            Vector3 worldCorrection = Vector3.up * (groundRoot.position.y - groundSink - bounds.min.y);
            if (centerOnLane)
            {
                worldCorrection.x = groundRoot.position.x - bounds.center.x;
            }

            Transform parent = visualRoot.parent;
            visualRoot.localPosition += parent != null
                ? parent.InverseTransformVector(worldCorrection)
                : worldCorrection;
        }
    }
}
