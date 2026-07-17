using System;

namespace CubeBlast.Core
{
    /// <summary>Builds a board from the level token grid.</summary>
    public static class BoardFactory
    {
        public static Board Create(LevelDefinition definition, IRandomProvider random)
        {
            if (definition.Tokens.Length != definition.Width * definition.Height)
            {
                throw new ArgumentException(
                    "Level " + definition.Number + " token count does not match grid size.");
            }

            var board = new Board(definition.Width, definition.Height);
            for (int i = 0; i < definition.Tokens.Length; i++)
            {
                // The grid list starts from the bottom-left and continues horizontally.
                var position = new GridPosition(i % definition.Width, i / definition.Width);
                CreateItem(board, definition.Tokens[i], position, random);
            }
            return board;
        }

        public static CubeColor RandomColor(IRandomProvider random)
        {
            return (CubeColor)random.Next(4);
        }

        private static void CreateItem(Board board, string token, GridPosition position, IRandomProvider random)
        {
            switch (token)
            {
                case "r": board.Place(new CubeItem(CubeColor.Red), position); break;
                case "g": board.Place(new CubeItem(CubeColor.Green), position); break;
                case "b": board.Place(new CubeItem(CubeColor.Blue), position); break;
                case "y": board.Place(new CubeItem(CubeColor.Yellow), position); break;
                case "rand": board.Place(new CubeItem(RandomColor(random)), position); break;
                case "hro": board.Place(new RocketItem(RocketOrientation.Horizontal), position); break;
                case "vro": board.Place(new RocketItem(RocketOrientation.Vertical), position); break;
                case "t": board.Place(new TntItem(), position); break;
                case "s": board.Place(new StoneItem(), position); break;
                case "v": board.Place(new VaseItem(), position); break;
                case "cbBL": board.Place(new ChaliceBoxItem(), position); break;
                case "cbBR":
                case "cbTL":
                case "cbTR":
                    // These cells belong to the Chalice Box placed at its bottom-left corner.
                    // The bottom-left token always comes first in the grid list.
                    if (!(board.ItemAt(position) is ChaliceBoxItem))
                    {
                        throw new ArgumentException("Orphan Chalice Box token at " + position);
                    }
                    break;
                default:
                    throw new ArgumentException("Unknown level token '" + token + "' at " + position);
            }
        }
    }
}
