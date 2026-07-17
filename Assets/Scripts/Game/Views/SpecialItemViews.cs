using CubeBlast.Core;

namespace CubeBlast.Game
{
    public sealed class RocketItemView : ItemView
    {
        public RocketItem Rocket { get { return (RocketItem)Item; } }

        protected override void OnInit()
        {
            SetSprite(GameAssets.Rocket(Rocket.Orientation));
        }

        public override void SpawnDestructionFx()
        {
            Fx.Burst(CenterWorldPosition, new[] { GameAssets.RocketStar }, 10, 0.4f * Board.CellSize, 6.5f, 4f, 0.55f);
        }
    }

    public sealed class TntItemView : ItemView
    {
        protected override void OnInit()
        {
            SetSprite(GameAssets.Tnt);
        }

        public override void SpawnDestructionFx()
        {
            Fx.Burst(CenterWorldPosition, Fx.TntShards(), 14, 0.44f * Board.CellSize, speed: 6f);
        }
    }
}
