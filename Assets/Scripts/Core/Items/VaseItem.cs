namespace CubeBlast.Core
{
    /// <summary>
    /// Vase: cleared with two damages, falls down, takes damage from adjacent
    /// blasts and special item explosions. Never takes more than one damage
    /// from a single source.
    /// </summary>
    public sealed class VaseItem : ObstacleItem
    {
        public const string Key = "vase";

        public VaseItem()
        {
            Health = GameRules.VaseHealth;
        }

        public override bool CanFall { get { return true; } }
        public override string GoalKey { get { return Key; } }

        public override DamageResult ApplyBlastDamage(int adjacentCubeCount)
        {
            //It takes no more than one damage from a single blast.
            return LoseHealth(1);
        }

        public override DamageResult ApplyExplosionDamage(int affectedCellCount)
        {
            return LoseHealth(1);
        }
    }
}
