namespace CubeBlast.Core
{
    /// <summary>
    /// Chalice Box: a static 2x2 obstacle with two phases.
    /// Door phase: every damage source deals exactly 1 damage, regardless of
    /// how many cells it hits. Breaking the door starts the chalice phase.
    /// Chalice phase: explosions deal damage equal to the number of affected
    /// box cells, adjacent blasts deal damage equal to the number of adjacent
    /// cubes; every point of damage collects one chalice. Cleared when all
    /// chalices are collected.
    /// </summary>
    public sealed class ChaliceBoxItem : ObstacleItem
    {
        public const string Key = "chalice";

        public bool IsDoorBroken { get; private set; }
        public int ChalicesCollected { get; private set; }
        public int ChalicesRemaining { get { return GameRules.ChalicesPerBox - ChalicesCollected; } }

        public ChaliceBoxItem()
        {
            Health = GameRules.ChaliceBoxDoorHealth;
        }

        public override int CellWidth { get { return GameRules.ChaliceBoxSize; } }
        public override int CellHeight { get { return GameRules.ChaliceBoxSize; } }
        public override bool CanFall { get { return false; } }
        public override string GoalKey { get { return Key; } }
        public override int RemainingGoalAmount { get { return ChalicesRemaining; } }

        public override DamageResult ApplyBlastDamage(int adjacentCubeCount)
        {
            if (!IsDoorBroken)
            {
                return DamageDoor();
            }
            return CollectChalices(adjacentCubeCount);
        }

        public override DamageResult ApplyExplosionDamage(int affectedCellCount)
        {
            if (!IsDoorBroken)
            {
                return DamageDoor();
            }
            return CollectChalices(affectedCellCount);
        }

        private DamageResult DamageDoor()
        {
            // Each damage source deals exactly 1 damage in the door phase.
            var result = LoseHealth(1);
            if (result.Destroyed)
            {
                // The box survives; only the door broke.
                IsDestroyed = false;
                IsDoorBroken = true;
                result.Destroyed = false;
                result.DoorBroken = true;
            }
            return result;
        }

        private DamageResult CollectChalices(int amount)
        {
            var result = new DamageResult();
            if (IsDestroyed || amount <= 0)
            {
                return result;
            }
            int collected = amount > ChalicesRemaining ? ChalicesRemaining : amount;
            ChalicesCollected += collected;
            result.Damage = collected;
            result.ChalicesCollected = collected;
            if (ChalicesRemaining <= 0)
            {
                IsDestroyed = true;
                result.Destroyed = true;
            }
            return result;
        }
    }
}
