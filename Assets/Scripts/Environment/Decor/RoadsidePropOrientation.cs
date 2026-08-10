using UnityEngine;

namespace JoburgRunner.Environment.Decor
{
    /// <summary>
    /// Describes how a roadside prefab's authored forward/up axes differ from
    /// Jozi Runner's socket convention. The decorator first calculates the
    /// pavement/road-facing rotation, then composes this local offset after it.
    ///
    /// Add this component to any future roadside prefab and tune Rotation Offset
    /// in the Inspector. No spawn-code or socket changes are required. Leave the
    /// offset at zero to preserve the prefab's existing orientation.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RoadsidePropOrientation : MonoBehaviour
    {
        [Tooltip("Local Euler offset applied after the socket's road-facing rotation. " +
                 "Use this to make the prefab's visible front face the road while " +
                 "keeping FBX axis/grounding corrections inside the visual child.")]
        [SerializeField] Vector3 rotationOffset;

        public Vector3 RotationOffset => rotationOffset;
        public Quaternion LocalOffset => Quaternion.Euler(rotationOffset);
        public bool RequiresPostRotationGrounding =>
            Mathf.Abs(Mathf.DeltaAngle(0f, rotationOffset.x)) > 0.01f ||
            Mathf.Abs(Mathf.DeltaAngle(0f, rotationOffset.z)) > 0.01f;

        public Quaternion ApplyAfter(Quaternion roadFacingRotation)
        {
            return roadFacingRotation * LocalOffset;
        }

#if UNITY_EDITOR
        public void SetRotationOffset(Vector3 value)
        {
            rotationOffset = value;
        }
#endif
    }
}
