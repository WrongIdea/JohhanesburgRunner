using UnityEngine;

namespace JoburgRunner
{
    /// <summary>
    /// Keeps a lightweight atmospheric mask ahead of the runner. The centre road
    /// corridor stays open for hazard readability while roadside spawn/recycle
    /// activity remains hidden. Also restores linear fog after district changes.
    /// </summary>
    public sealed class RoadsideSpawnHaze : MonoBehaviour
    {
        [SerializeField] Transform runner;
        [SerializeField] float forwardDistance = 123f;
        [SerializeField] Color fogColor = new Color(0.72f, 0.79f, 0.86f);
        [SerializeField] float fogStart = 110f;
        [SerializeField] float fogEnd = 155f;

        void LateUpdate()
        {
            if (runner != null)
            {
                Vector3 position = transform.position;
                position.z = runner.position.z + forwardDistance;
                transform.position = position;
            }

            // Environment zones may apply their own haze earlier in the frame.
            // Reassert the gameplay-safe linear distances consistently.
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = fogColor;
            RenderSettings.fogStartDistance = fogStart;
            RenderSettings.fogEndDistance = fogEnd;
        }
    }
}
