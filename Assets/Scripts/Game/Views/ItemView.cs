using CubeBlast.Core;
using UnityEngine;

namespace CubeBlast.Game
{
    /// <summary>
    /// Base class of all board item visuals. Each core <see cref="BoardItem"/>
    /// has exactly one view, tracked by item id, that mirrors the simulation.
    /// </summary>
    public abstract class ItemView : MonoBehaviour
    {
        public BoardItem Item { get; private set; }

        protected BoardView Board { get; private set; }
        protected SpriteRenderer Renderer { get; private set; }

        /// <summary>Scale-animation target, kept separate from the root so tweens do not fight.</summary>
        public Transform Body { get; private set; }

        public virtual void Init(BoardView board, BoardItem item)
        {
            Board = board;
            Item = item;

            var body = new GameObject("Body");
            body.transform.SetParent(transform, false);
            Body = body.transform;
            Renderer = body.AddComponent<SpriteRenderer>();

            OnInit();
            SyncPosition();
            UpdateSorting();
        }

        /// <summary>Subclasses set their sprites here.</summary>
        protected abstract void OnInit();

        /// <summary>World-space center of this item (multi-cell aware).</summary>
        public Vector3 CenterWorldPosition
        {
            get
            {
                var a = Board.CellToWorld(Item.Origin);
                var b = Board.CellToWorld(Item.Origin.Offset(Item.CellWidth - 1, Item.CellHeight - 1));
                return (a + b) * 0.5f;
            }
        }

        public void SyncPosition()
        {
            transform.position = CenterWorldPosition;
        }

        public void UpdateSorting()
        {
            SetSortingOrder(Board.SortingOrderForRow(Item.Origin.Y));
        }

        protected virtual void SetSortingOrder(int order)
        {
            Renderer.sortingOrder = order;
        }

        /// <summary>Sets the sprite and scales it to fit the item's footprint.</summary>
        protected void SetSprite(Sprite sprite, float fill = 0.96f)
        {
            Renderer.sprite = sprite;
            FitToFootprint(Renderer, sprite, fill);
        }

        protected void FitToFootprint(SpriteRenderer renderer, Sprite sprite, float fill)
        {
            if (sprite == null || renderer == null)
            {
                return;
            }
            float targetWidth = Item.CellWidth * Board.CellSize * fill;
            float targetHeight = Item.CellHeight * Board.CellSize * fill;
            var size = sprite.bounds.size;
            float scale = Mathf.Min(targetWidth / size.x, targetHeight / size.y);
            renderer.transform.localScale = new Vector3(scale, scale, 1f);
        }

        /// <summary>Feedback for taps that are not a valid move.</summary>
        public void Wiggle()
        {
            Tween.Punch(transform, -0.12f, 0.16f);
        }

        /// <summary>Obstacle views override this to react to damage events.</summary>
        public virtual void OnDamaged(ObstacleDamage damage)
        {
        }

        /// <summary>Shards spawned when this item is destroyed by an explosion or blast.</summary>
        public virtual void SpawnDestructionFx()
        {
        }
    }
}
