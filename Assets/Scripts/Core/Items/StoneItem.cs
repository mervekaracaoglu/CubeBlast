namespace CubeBlast.Core
{
    /// <summary>
    /// Stone: static obstacle, immune to blasts, cleared by one damage
    /// from any special item explosion.
    /// </summary>
    public sealed class StoneItem : ObstacleItem
    {
        public const string Key = "stone";

        public StoneItem()
        {
            Health = GameRules.StoneHealth;
        }

        public override bool CanFall { get { return false; } }
        public override string GoalKey { get { return Key; } }

        public override DamageResult ApplyExplosionDamage(int affectedCellCount)
        {
            return LoseHealth(1);
        }
    }
}
