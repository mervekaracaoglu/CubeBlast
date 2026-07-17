namespace CubeBlast.Core
{
    /// <summary>Outcome of applying one damage source to an obstacle.</summary>
    public struct DamageResult
    {
        public int Damage;
        public int ChalicesCollected;
        public bool Destroyed;
        /// <summary>True the moment the Chalice Box door breaks and the chalice phase starts.</summary>
        public bool DoorBroken;

        public bool TookDamage { get { return Damage > 0; } }

        public static DamageResult None { get { return new DamageResult(); } }
    }

    /// <summary>
    /// Base class for level goals. Obstacles decide themselves how much damage
    /// each kind of source deals to them (polymorphism instead of switch-cases).
    /// </summary>
    public abstract class ObstacleItem : BoardItem
    {
        public int Health { get; protected set; }

        /// <summary>Groups obstacles of the same kind in the goal UI.</summary>
        public abstract string GoalKey { get; }

        /// <summary>How many goal units this obstacle still holds (for the goal counters).</summary>
        public virtual int RemainingGoalAmount { get { return IsDestroyed ? 0 : 1; } }

        /// <summary>
        /// Damage from a cube blast happening in orthogonally adjacent cells.
        /// <paramref name="adjacentCubeCount"/> is how many cubes of the blasted
        /// group touch this obstacle.
        /// </summary>
        public virtual DamageResult ApplyBlastDamage(int adjacentCubeCount)
        {
            return DamageResult.None;
        }

        /// <summary>
        /// Damage from a single special item explosion (one rocket, one TNT, one combo blast).
        /// <paramref name="affectedCellCount"/> is how many of this obstacle's cells
        /// the explosion covered.
        /// </summary>
        public abstract DamageResult ApplyExplosionDamage(int affectedCellCount);

        protected DamageResult LoseHealth(int amount)
        {
            var result = new DamageResult();
            if (IsDestroyed || amount <= 0)
            {
                return result;
            }
            int applied = amount > Health ? Health : amount;
            Health -= applied;
            result.Damage = applied;
            if (Health <= 0)
            {
                IsDestroyed = true;
                result.Destroyed = true;
            }
            return result;
        }
    }
}
