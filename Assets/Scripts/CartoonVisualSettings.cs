using UnityEngine;

namespace JoburgRunner
{
    [CreateAssetMenu(menuName = "Jozi Runner/Cartoon Visual Settings", fileName = "CartoonVisualSettings")]
    public sealed class CartoonVisualSettings : ScriptableObject
    {
        public bool enabled = false;
        [Range(0f, 1f)] public float saturationBoost = 0.18f;
    }
}
