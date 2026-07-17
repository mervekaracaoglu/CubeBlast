namespace CubeBlast.Core
{
    /// <summary>
    /// Engine-independent description of a level. The Unity layer builds this
    /// from the level JSON files; the core never touches file formats.
    /// </summary>
    public sealed class LevelDefinition
    {
        public readonly int Number;
        public readonly int Width;
        public readonly int Height;
        public readonly int MoveCount;

        /// <summary>Cell tokens starting from the bottom-left, row by row, ending at the top-right.</summary>
        public readonly string[] Tokens;

        public LevelDefinition(int number, int width, int height, int moveCount, string[] tokens)
        {
            Number = number;
            Width = width;
            Height = height;
            MoveCount = moveCount;
            Tokens = tokens;
        }
    }
}
