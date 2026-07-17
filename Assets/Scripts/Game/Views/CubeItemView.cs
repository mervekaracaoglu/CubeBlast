using CubeBlast.Core;

namespace CubeBlast.Game
{
    public sealed class CubeItemView : ItemView
    {
        private CubeItem Cube { get { return (CubeItem)Item; } }
        private SpecialHint _hint = SpecialHint.None;

        protected override void OnInit()
        {
            SetSprite(GameAssets.Cube(Cube.Color));
        }

        /// <summary>Cube groups display a special item logo when they are big enough to create one.</summary>
        public void ApplyHint(SpecialHint hint)
        {
            if (hint == _hint)
            {
                return;
            }
            _hint = hint;
            switch (hint)
            {
                case SpecialHint.Tnt:
                    SetSprite(GameAssets.CubeTntHint(Cube.Color));
                    break;
                case SpecialHint.Rocket:
                    SetSprite(GameAssets.CubeRocketHint(Cube.Color));
                    break;
                default:
                    SetSprite(GameAssets.Cube(Cube.Color));
                    break;
            }
        }

        public override void SpawnDestructionFx()
        {
            Fx.Burst(CenterWorldPosition, Fx.CubeShards(Cube.Color), 8, 0.44f * Board.CellSize, speed: 5.5f);
        }
    }
}
