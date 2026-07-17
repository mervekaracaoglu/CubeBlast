namespace CubeBlast.Core
{
    /// <summary>
    /// Base class for tappable special items (Rocket, TNT). Special items explode
    /// when tapped or when damaged by another special item's explosion.
    /// </summary>
    public abstract class SpecialItem : BoardItem
    {
        public override bool CanFall { get { return true; } }
    }

    public sealed class RocketItem : SpecialItem
    {
        public RocketOrientation Orientation { get; private set; }

        public RocketItem(RocketOrientation orientation)
        {
            Orientation = orientation;
        }
    }

    public sealed class TntItem : SpecialItem
    {
    }
}
