namespace CubeBlast.Core
{
    /// <summary>
    /// Central place for all gameplay tuning values so designers can change
    /// the rules without touching the systems that use them.
    /// </summary>
    public static class GameRules
    {
        /// <summary>Minimum cube group size that can be blasted.</summary>
        public const int MinBlastGroupSize = 2;

        /// <summary>Group size that creates a Rocket (4 or 5 cubes).</summary>
        public const int RocketGroupSize = 4;

        /// <summary>Group size that creates a TNT (6 or more cubes).</summary>
        public const int TntGroupSize = 6;

        /// <summary>TNT explodes in a 5x5 area (half extent = 2).</summary>
        public const int TntRadius = 2;

        /// <summary>TNT-TNT combo explodes in a 7x7 area (half extent = 3).</summary>
        public const int TntTntRadius = 3;

        public const int VaseHealth = 2;
        public const int StoneHealth = 1;

        /// <summary>
        /// Hits needed to break the Chalice Box door.
        /// </summary>
        public const int ChaliceBoxDoorHealth = 1;

        /// <summary>Total chalices that must be collected from a single Chalice Box.</summary>
        public const int ChalicesPerBox = 10;

        public const int ChaliceBoxSize = 2; // 2x2 item
    }
}
