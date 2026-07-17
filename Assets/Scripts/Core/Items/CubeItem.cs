namespace CubeBlast.Core
{
    /// <summary>A regular coloured cube. Blasted in groups of 2+ same-coloured neighbours.</summary>
    public sealed class CubeItem : BoardItem
    {
        public CubeColor Color { get; private set; }

        public CubeItem(CubeColor color)
        {
            Color = color;
        }

        public override bool CanFall { get { return true; } }
    }
}
