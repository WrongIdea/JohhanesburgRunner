using TMPro;
using UnityEngine;

namespace JoburgRunner.Environment.Decor
{
    /// <summary>
    /// Paints randomised destination names onto a roadside direction sign. The
    /// left- and right-pointing boards each draw a city from their own pool, and
    /// the pairing is re-rolled every time the sign is enabled – so pooled re-use
    /// and neighbouring junctions never show the same combination twice in a row.
    ///
    /// Labels are generated in code (world-space <see cref="TextMeshPro"/>) so the
    /// Meshy sign mesh needs no hand-placed text children. The board offsets are
    /// serialized because the exact face position depends on the imported model –
    /// tune <see cref="leftLabelOffset"/>/<see cref="rightLabelOffset"/> in the
    /// prefab until each name sits on its arrow, then the values stick.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DirectionSignCities : MonoBehaviour
    {
        [Header("Destination pools")]
        [Tooltip("Cities shown on the left-pointing board (northern/eastern suburbs).")]
        [SerializeField]
        string[] leftCities =
        {
            "Sandton", "Pretoria", "Centurion", "Midrand",
            "Fourways", "Woodmead", "Mafikeng"
        };

        [Tooltip("Cities shown on the right-pointing board (southern/eastern towns).")]
        [SerializeField]
        string[] rightCities =
        {
            "Soweto", "Vereeniging", "Alberton", "Springs", "Vanderbijlpark"
        };

        [Header("Left board (sign-local space)")]
        [SerializeField] Vector3 leftLabelOffset = new Vector3(0f, 0.62f, 0.06f);
        [SerializeField] Vector3 leftLabelEuler = Vector3.zero;

        [Header("Right board (sign-local space)")]
        [SerializeField] Vector3 rightLabelOffset = new Vector3(0f, 0.34f, 0.06f);
        [SerializeField] Vector3 rightLabelEuler = Vector3.zero;

        [Header("Text appearance")]
        [Tooltip("Local font size. The prefab is scaled up, so keep this small.")]
        [SerializeField] float fontSize = 0.75f;
        [SerializeField] Color textColor = Color.white;
        [Tooltip("Append a directional chevron after each city name.")]
        [SerializeField] bool showArrow = true;
        [Tooltip("Optional TMP font override; falls back to the project default.")]
        [SerializeField] TMP_FontAsset font;

        TextMeshPro leftLabel;
        TextMeshPro rightLabel;

        // Rolling counter so two signs enabled on the same frame still differ even
        // if Unity's RNG has not advanced between them.
        static int rollSalt;

        void OnEnable() => Repaint();

        /// <summary>
        /// Re-rolls both destinations. Public so a spawner can force a fresh
        /// pairing when it re-dresses a pooled junction tile.
        /// </summary>
        public void Repaint()
        {
            EnsureLabels();
            leftLabel.text = Compose(Pick(leftCities), true);
            rightLabel.text = Compose(Pick(rightCities), false);
        }

        string Pick(string[] pool)
        {
            if (pool == null || pool.Length == 0)
            {
                return string.Empty;
            }

            int index = (Random.Range(0, pool.Length) + rollSalt++) % pool.Length;
            return pool[index];
        }

        string Compose(string city, bool pointsLeft)
        {
            if (!showArrow || string.IsNullOrEmpty(city))
            {
                return city;
            }

            return pointsLeft ? "‹ " + city : city + " ›";
        }

        void EnsureLabels()
        {
            if (leftLabel == null)
            {
                leftLabel = CreateLabel("DestinationLeft", leftLabelOffset, leftLabelEuler);
            }
            if (rightLabel == null)
            {
                rightLabel = CreateLabel("DestinationRight", rightLabelOffset, rightLabelEuler);
            }
        }

        TextMeshPro CreateLabel(string name, Vector3 offset, Vector3 euler)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            go.transform.localPosition = offset;
            go.transform.localRotation = Quaternion.Euler(euler);

            var tmp = go.AddComponent<TextMeshPro>();
            if (font != null)
            {
                tmp.font = font;
            }
            tmp.fontSize = fontSize;
            tmp.color = textColor;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.raycastTarget = false;

            // Sizing the RectTransform keeps auto-layout from wrapping long names
            // like "Vanderbijlpark" across the narrow board.
            var rect = tmp.rectTransform;
            rect.sizeDelta = new Vector2(6f, 1.2f);
            return tmp;
        }
    }
}
