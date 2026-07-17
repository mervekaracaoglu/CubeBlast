using System.Collections.Generic;

namespace CubeBlast.Core
{
    /// <summary>Damage applied to one obstacle by one damage source.</summary>
    public struct ObstacleDamage
    {
        public ObstacleItem Obstacle;
        public int Damage;
        public int ChalicesCollected;
        public bool Destroyed;
        public bool DoorBroken;
    }

    public struct FallMove
    {
        public BoardItem Item;
        public GridPosition From;
        public GridPosition To;
    }

    public struct SpawnedCube
    {
        public CubeItem Cube;
        public GridPosition Target;
        /// <summary>Virtual row above the board the cube starts falling from (>= board height).</summary>
        public int SpawnRow;
    }

    /// <summary>
    /// One step of a resolved turn. The view layer plays the steps of a
    /// <see cref="TurnResult"/> in order to animate exactly what the simulation did.
    /// </summary>
    public abstract class TurnStep
    {
    }

    /// <summary>A cube group was blasted; optionally a special item was created at the tap cell.</summary>
    public sealed class BlastStep : TurnStep
    {
        public GridPosition TapPosition;
        public List<CubeItem> Cubes = new List<CubeItem>();
        public List<ObstacleDamage> Damages = new List<ObstacleDamage>();
        /// <summary>Created special item, already placed at the tap cell. Null for plain blasts.</summary>
        public SpecialItem CreatedSpecial;
    }

    /// <summary>Adjacent special items flew to the tap cell to form a combo.</summary>
    public sealed class ComboGatherStep : TurnStep
    {
        public GridPosition Center;
        public List<SpecialItem> Items = new List<SpecialItem>();
        public ComboKind Kind;
    }

    /// <summary>A single explosion primitive (one rocket, one TNT area or one combo area).</summary>
    public sealed class ExplosionStep : TurnStep
    {
        public ExplosionKind Kind;
        public GridPosition Center;
        public RocketOrientation Orientation;
        /// <summary>Explosions with the same wave index happen at the same time; chained ones come later.</summary>
        public int Wave;
        /// <summary>The special item that exploded, or null for combo primitives.</summary>
        public SpecialItem Source;
        /// <summary>Cells covered by this explosion, ordered by distance from the center.</summary>
        public List<GridPosition> AffectedCells = new List<GridPosition>();
        public List<CubeItem> DestroyedCubes = new List<CubeItem>();
        /// <summary>Special items this explosion detonated in chain (their own steps follow).</summary>
        public List<SpecialItem> TriggeredSpecials = new List<SpecialItem>();
        public List<ObstacleDamage> Damages = new List<ObstacleDamage>();
    }

    /// <summary>Items fell down and new cubes entered from the top of the grid.</summary>
    public sealed class GravityStep : TurnStep
    {
        public List<FallMove> Falls = new List<FallMove>();
        public List<SpawnedCube> Spawns = new List<SpawnedCube>();
    }

    /// <summary>No valid move was available, cubes were re-arranged.</summary>
    public sealed class ShuffleStep : TurnStep
    {
        public List<FallMove> Moves = new List<FallMove>();
    }

    /// <summary>Everything that happened as the result of one tap.</summary>
    public sealed class TurnResult
    {
        public bool IsValidMove;
        /// <summary>Set when the tap hit something but it was not a playable move (e.g. a single cube).</summary>
        public BoardItem RejectedItem;
        public List<TurnStep> Steps = new List<TurnStep>();
        public LevelState State;
        public int MovesLeft;
    }
}
