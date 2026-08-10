using UnityEngine;

namespace JoburgRunner
{
    /// <summary>Continuously rotates this transform on a fixed local axis. Used for Ubuntu Pulse's independently-spinning energy rings.</summary>
    public class SpinAxis : MonoBehaviour
    {
        [SerializeField] Vector3 axis = Vector3.up;
        [SerializeField] float degreesPerSecond = 45f;

        void Update()
        {
            transform.Rotate(axis, degreesPerSecond * Time.deltaTime, Space.Self);
        }
    }
}
