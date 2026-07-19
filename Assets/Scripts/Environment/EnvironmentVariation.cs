using System.Collections.Generic;
using UnityEngine;

namespace JoburgRunner.Environment
{
    /// <summary>
    /// Per-segment dressing options for a zone. Each list is a pool the
    /// variation picker draws from as road segments recycle, so the same
    /// stretch of street rarely looks identical twice. All fields are optional
    /// — an empty pool means "leave that dimension at the segment's default".
    ///
    /// This type is pure data; <see cref="Pick"/> chooses one combination and
    /// the environment integration layer applies it to a segment's renderers.
    /// </summary>
    [System.Serializable]
    public struct EnvironmentVariation
    {
        [Header("Materials & Colours")]
        public Color[] buildingColors;
        public Material[] windowMaterials;
        public Material[] wallMaterials;
        public Color[] vehicleColors;

        [Header("Signage & Decals")]
        [Tooltip("Fictional Johannesburg-inspired shop names — never real protected brands.")]
        public string[] shopSigns;
        public Texture2D[] billboardTextures;
        public Texture2D[] graffitiDecals;

        [Header("Placement Density")]
        [Tooltip("Metres between pavement trees; x=min (dense), y=max (sparse).")]
        public Vector2 treeSpacingRange;
        [Tooltip("Metres between streetlights; x=min, y=max.")]
        public Vector2 streetlightSpacingRange;

        [Header("Wear & Weather")]
        [Tooltip("Road-wear intensity range 0..1 blended into the asphalt tint.")]
        public Vector2 roadWearRange;
        public Gradient weatherTintRange;

        [Header("Anti-Repetition")]
        [Tooltip("How many of the most-recent combinations to avoid re-picking.")]
        [Min(0)] public int avoidRecentCount;

        public static EnvironmentVariation Default => new EnvironmentVariation
        {
            buildingColors = new Color[0],
            windowMaterials = new Material[0],
            wallMaterials = new Material[0],
            vehicleColors = new Color[0],
            shopSigns = new string[0],
            billboardTextures = new Texture2D[0],
            graffitiDecals = new Texture2D[0],
            treeSpacingRange = new Vector2(6f, 12f),
            streetlightSpacingRange = new Vector2(12f, 20f),
            roadWearRange = new Vector2(0f, 0.4f),
            weatherTintRange = null,
            avoidRecentCount = 3,
        };

        /// <summary>
        /// One resolved dressing combination for a single segment. Index -1
        /// means "no choice for this dimension" (empty pool).
        /// </summary>
        public struct Selection
        {
            public int buildingColorIndex;
            public int windowMaterialIndex;
            public int wallMaterialIndex;
            public int shopSignIndex;
            public int billboardIndex;
            public int graffitiIndex;
            public float treeSpacing;
            public float streetlightSpacing;
            public float roadWear;
            public Color weatherTint;

            public Color BuildingColor(EnvironmentVariation v) =>
                buildingColorIndex >= 0 ? v.buildingColors[buildingColorIndex] : Color.white;
            public Material WindowMaterial(EnvironmentVariation v) =>
                windowMaterialIndex >= 0 ? v.windowMaterials[windowMaterialIndex] : null;
            public Material WallMaterial(EnvironmentVariation v) =>
                wallMaterialIndex >= 0 ? v.wallMaterials[wallMaterialIndex] : null;
            public string ShopSign(EnvironmentVariation v) =>
                shopSignIndex >= 0 ? v.shopSigns[shopSignIndex] : null;
            public Texture2D Billboard(EnvironmentVariation v) =>
                billboardIndex >= 0 ? v.billboardTextures[billboardIndex] : null;
            public Texture2D Graffiti(EnvironmentVariation v) =>
                graffitiIndex >= 0 ? v.graffitiDecals[graffitiIndex] : null;
        }

        /// <summary>
        /// Picks one dressing combination, avoiding recent repeats via the
        /// supplied <paramref name="state"/> (create one per running route and
        /// reuse it so the anti-repetition memory persists across segments).
        /// </summary>
        public Selection Pick(VariationState state)
        {
            return new Selection
            {
                buildingColorIndex = state.buildingColors.Next(SafeLength(buildingColors)),
                windowMaterialIndex = state.windowMaterials.Next(SafeLength(windowMaterials)),
                wallMaterialIndex = state.wallMaterials.Next(SafeLength(wallMaterials)),
                shopSignIndex = state.shopSigns.Next(SafeLength(shopSigns)),
                billboardIndex = state.billboards.Next(SafeLength(billboardTextures)),
                graffitiIndex = state.graffiti.Next(SafeLength(graffitiDecals)),
                treeSpacing = RandomInRange(treeSpacingRange, 8f),
                streetlightSpacing = RandomInRange(streetlightSpacingRange, 16f),
                roadWear = RandomInRange(roadWearRange, 0f),
                weatherTint = weatherTintRange != null
                    ? weatherTintRange.Evaluate(Random.value)
                    : Color.white,
            };
        }

        static int SafeLength(System.Array array) => array != null ? array.Length : 0;

        static float RandomInRange(Vector2 range, float fallback)
        {
            if (range == Vector2.zero)
            {
                return fallback;
            }

            return Random.Range(Mathf.Min(range.x, range.y), Mathf.Max(range.x, range.y));
        }

        /// <summary>
        /// Holds the per-dimension anti-repetition memory for one route.
        /// Sized from a variation so its pools stay in sync.
        /// </summary>
        public sealed class VariationState
        {
            public readonly NoRepeatPicker buildingColors;
            public readonly NoRepeatPicker windowMaterials;
            public readonly NoRepeatPicker wallMaterials;
            public readonly NoRepeatPicker shopSigns;
            public readonly NoRepeatPicker billboards;
            public readonly NoRepeatPicker graffiti;

            public VariationState(int avoidRecent)
            {
                buildingColors = new NoRepeatPicker(avoidRecent);
                windowMaterials = new NoRepeatPicker(avoidRecent);
                wallMaterials = new NoRepeatPicker(avoidRecent);
                shopSigns = new NoRepeatPicker(avoidRecent);
                billboards = new NoRepeatPicker(avoidRecent);
                graffiti = new NoRepeatPicker(avoidRecent);
            }
        }
    }

    /// <summary>
    /// Reusable weighted-ish random index picker that avoids re-emitting any
    /// of the last N results. Zero per-pick allocation once warmed. Shared by
    /// environment variation today and available to any spawner that wants to
    /// stop the same option appearing back-to-back.
    /// </summary>
    public sealed class NoRepeatPicker
    {
        readonly int memory;
        readonly Queue<int> recent = new Queue<int>();

        public NoRepeatPicker(int memory)
        {
            this.memory = Mathf.Max(0, memory);
        }

        /// <summary>
        /// Returns an index in [0, count), or -1 when count is 0. Avoids the
        /// last <c>memory</c> results while enough distinct options remain.
        /// </summary>
        public int Next(int count)
        {
            if (count <= 0)
            {
                return -1;
            }

            if (count == 1)
            {
                return 0;
            }

            int effectiveMemory = Mathf.Min(memory, count - 1);
            int index;
            int guard = 0;
            do
            {
                index = Random.Range(0, count);
            }
            while (recent.Contains(index) && ++guard < 16);

            recent.Enqueue(index);
            while (recent.Count > effectiveMemory)
            {
                recent.Dequeue();
            }

            return index;
        }
    }
}
