using System.Collections.Generic;
using CubeBlast.Core;
using UnityEngine;

namespace CubeBlast.Game
{
    /// <summary>
    /// Builds and maintains the visual board: grid background, one view per
    /// core item, and grid-to-world conversions.
    /// </summary>
    public sealed class BoardView : MonoBehaviour
    {
        public float CellSize { get; private set; }

        private Board _board;
        private readonly Dictionary<int, ItemView> _views = new Dictionary<int, ItemView>();

        public void Build(Board board, float cellSize)
        {
            _board = board;
            CellSize = cellSize;

            CreateGridBackground();
            foreach (var item in board.Items)
            {
                CreateView(item);
            }
        }

        private void CreateGridBackground()
        {
            var go = new GameObject("GridBackground");
            go.transform.SetParent(transform, false);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = GameAssets.GridBackground;
            renderer.sortingOrder = -10;
            renderer.color = new Color(1f, 1f, 1f, 0.95f);
            if (renderer.sprite != null)
            {
                float padding = 0.22f * CellSize;
                float width = _board.Width * CellSize + padding * 2f;
                float height = _board.Height * CellSize + padding * 2f;
                if (renderer.sprite.border.sqrMagnitude > 0f)
                {
                    renderer.drawMode = SpriteDrawMode.Sliced;
                    renderer.size = new Vector2(width, height);
                }
                else
                {
                    var size = renderer.sprite.bounds.size;
                    go.transform.localScale = new Vector3(width / size.x, height / size.y, 1f);
                }
            }
        }

        // ------------------------------------------------------------- views

        public ItemView CreateView(BoardItem item)
        {
            var go = new GameObject(item.GetType().Name + "_" + item.Id);
            go.transform.SetParent(transform, false);
            ItemView view = AddViewComponent(go, item);
            view.Init(this, item);
            _views[item.Id] = view;
            return view;
        }

        private static ItemView AddViewComponent(GameObject go, BoardItem item)
        {
            if (item is CubeItem) { return go.AddComponent<CubeItemView>(); }
            if (item is RocketItem) { return go.AddComponent<RocketItemView>(); }
            if (item is TntItem) { return go.AddComponent<TntItemView>(); }
            if (item is VaseItem) { return go.AddComponent<VaseItemView>(); }
            if (item is StoneItem) { return go.AddComponent<StoneItemView>(); }
            if (item is ChaliceBoxItem) { return go.AddComponent<ChaliceBoxView>(); }
            throw new System.ArgumentException("No view registered for " + item.GetType().Name);
        }

        public ItemView GetView(BoardItem item)
        {
            ItemView view;
            _views.TryGetValue(item.Id, out view);
            return view;
        }

        public void RemoveView(BoardItem item, bool withFx)
        {
            var view = GetView(item);
            if (view == null)
            {
                return;
            }
            _views.Remove(item.Id);
            if (withFx && view.Body != null)
            {
                view.SpawnDestructionFx();
                // Sharp direct shrink over 0.08s for a very snappy pop out of existence
                Tween.Scale(view.Body, Vector3.zero, 0.08f, Ease.InQuad, 0f, () =>
                {
                    if (view != null && view.gameObject != null)
                    {
                        Destroy(view.gameObject);
                    }
                });
            }
            else
            {
                Destroy(view.gameObject);
            }
        }

        public IEnumerable<ItemView> Views
        {
            get { return _views.Values; }
        }

        // ---------------------------------------------------- grid <-> world

        public Vector3 CellToLocal(GridPosition position)
        {
            float x = (position.X - (_board.Width - 1) * 0.5f) * CellSize;
            float y = (position.Y - (_board.Height - 1) * 0.5f) * CellSize;
            return new Vector3(x, y, 0f);
        }

        public Vector3 CellToWorld(GridPosition position)
        {
            return transform.TransformPoint(CellToLocal(position));
        }

        public bool TryWorldToCell(Vector3 worldPosition, out GridPosition position)
        {
            var local = transform.InverseTransformPoint(worldPosition);
            int x = Mathf.FloorToInt(local.x / CellSize + _board.Width * 0.5f);
            int y = Mathf.FloorToInt(local.y / CellSize + _board.Height * 0.5f);
            position = new GridPosition(x, y);
            return x >= 0 && x < _board.Width && y >= 0 && y < _board.Height;
        }

        /// <summary>Lower rows draw in front, so falling items slide behind settled ones.</summary>
        public int SortingOrderForRow(int y)
        {
            return 40 - y;
        }

        public float BoardWorldWidth { get { return _board.Width * CellSize; } }
        public float BoardWorldHeight { get { return _board.Height * CellSize; } }
    }
}
