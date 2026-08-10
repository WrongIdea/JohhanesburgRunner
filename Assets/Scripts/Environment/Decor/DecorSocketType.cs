namespace JoburgRunner.Environment.Decor
{
    /// <summary>
    /// The kind of decoration a <see cref="DecorSocket"/> anchors. A road tile
    /// exposes a fixed set of sockets; the active <see cref="DistrictDecorProfile"/>
    /// decides – per socket type – whether and what to spawn. This keeps prop
    /// placement to hand-authored, gameplay-safe positions while letting the mix
    /// vary by district and by segment seed.
    /// </summary>
    public enum DecorSocketType
    {
        /// <summary>Sidewalk planting point for jacaranda trees.</summary>
        Tree = 0,

        /// <summary>Kerb-side street lamp (alternated left/right by the decorator).</summary>
        Lamp = 1,

        /// <summary>Stand-alone bench (parks/open spaces only – bus stops cluster their own).</summary>
        Bench = 2,

        /// <summary>Rubbish bin (usually part of a bus-stop cluster).</summary>
        Bin = 3,

        /// <summary>Traffic light. Only populated when the tile is flagged an intersection.</summary>
        TrafficLight = 4,

        /// <summary>Roadside billboard (commercial districts only).</summary>
        Billboard = 5,

        /// <summary>Bus stop anchor. Spawning one also drops a bench, bin and lamp nearby.</summary>
        BusStop = 6,

        /// <summary>
        /// Roadside hero-building anchor at the CBD building line. Never filled by the
        /// generic district profile; the <see cref="EnvironmentDecorDirector"/> drives
        /// a sparse, no-repeat, side-alternating spawn from a HeroBuildingSet and hides
        /// only the placeholders this socket overlaps.
        /// </summary>
        HeroBuilding = 7,

        /// <summary>Visual-only pedestrian crossing an active zebra crossing.</summary>
        Pedestrian = 8,

        /// <summary>Waving pedestrian beside a traffic light without zebra markings.</summary>
        WavingPedestrian = 9,

        /// <summary>Concrete planter placed on either pavement.</summary>
        Planter = 10,

        /// <summary>Roadside electrical utility cabinet placed on either pavement.</summary>
        UtilityBox = 11,

        /// <summary>
        /// Pedestrian walking along the pavement in the opposite direction to the
        /// player (world -Z). Driven by a <see cref="PavementWalker"/> mover rather
        /// than standing still. Purely visual and off-lane, so it never collides
        /// with the runner.
        /// </summary>
        WalkingPedestrian = 12
    }
}
