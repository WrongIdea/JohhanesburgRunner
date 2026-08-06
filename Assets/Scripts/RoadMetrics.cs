namespace JoburgRunner
{
    /// <summary>Shared production dimensions for the three-lane gameplay road.</summary>
    public static class RoadMetrics
    {
        public const float LaneSpacing = 2.38f;
        public const float RoadWidth = 8.45f;
        public const float RoadHalfWidth = RoadWidth * 0.5f;
        public const float LaneKeepOut = 4.05f;
        public const float SegmentLength = 30f;

        public static float LaneX(int lane) => (lane - 1) * LaneSpacing;
    }
}
