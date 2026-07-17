using CubeBlast.Core;
using UnityEngine;

namespace CubeBlast.Game
{
    public sealed class VaseItemView : ItemView
    {
        protected override void OnInit()
        {
            SetSprite(GameAssets.Vase(false));
        }

        public override void OnDamaged(ObstacleDamage damage)
        {
            Fx.Burst(CenterWorldPosition, Fx.VaseShards(), damage.Destroyed ? 14 : 7, 0.52f * Board.CellSize, speed: 5.5f);
            if (!damage.Destroyed)
            {
                SetSprite(GameAssets.Vase(true)); // cracked state after the first damage
                Tween.Punch(Body, 0.15f, 0.2f);
            }
        }

        public override void SpawnDestructionFx()
        {
            Fx.Burst(CenterWorldPosition, Fx.VaseShards(), 14, 0.52f * Board.CellSize, speed: 5.5f);
        }
    }

    public sealed class StoneItemView : ItemView
    {
        protected override void OnInit()
        {
            SetSprite(GameAssets.Stone, 1f);
        }

        public override void OnDamaged(ObstacleDamage damage)
        {
            Fx.Burst(CenterWorldPosition, Fx.StoneShards(), 14, 0.54f * Board.CellSize, speed: 5.5f);
        }

        public override void SpawnDestructionFx()
        {
            Fx.Burst(CenterWorldPosition, Fx.StoneShards(), 14, 0.54f * Board.CellSize, speed: 5.5f);
        }
    }

    /// <summary>
    /// The 2x2 Chalice Box. Shows closed doors during the door phase; once the
    /// door breaks it reveals the chalice and a counter of chalices left to collect.
    /// </summary>
    public sealed class ChaliceBoxView : ItemView
    {
        private ChaliceBoxItem Box { get { return (ChaliceBoxItem)Item; } }

        private SpriteRenderer _doors;
        private readonly SpriteRenderer[] _chalices = new SpriteRenderer[10];
        private Vector3 _chaliceBaseScale;

        protected override void OnInit()
        {
            SetSprite(GameAssets.ChaliceBoxBg, 1f);

            _doors = CreateOverlay("Doors", GameAssets.ChaliceBoxDoors, 1f);

            // Calculate and cache the correct base scale of chalices relative to the cabinet footprint
            float targetWidth = Item.CellWidth * Board.CellSize * 0.30f;
            float targetHeight = Item.CellHeight * Board.CellSize * 0.30f;
            var chaliceSpriteSize = GameAssets.Chalice.bounds.size;
            float scaleVal = Mathf.Min(targetWidth / chaliceSpriteSize.x, targetHeight / chaliceSpriteSize.y);
            _chaliceBaseScale = new Vector3(scaleVal, scaleVal, 1f);

            float[] xOffsets = { -0.52f, -0.26f, 0f, 0.26f, 0.52f };

            for (int i = 0; i < 10; i++)
            {
                int shelfIndex = i / 5;
                int slotIndex = i % 5;
                float x = xOffsets[slotIndex] * Board.CellSize;
                
                float dip = 0f;
                if (slotIndex == 2) dip = -0.05f * Board.CellSize;
                else if (slotIndex == 1 || slotIndex == 3) dip = -0.025f * Board.CellSize;

                float y = (shelfIndex == 0 ? 0.38f : -0.48f) * Board.CellSize + dip;

                var go = new GameObject($"Chalice_{i}");
                go.transform.SetParent(transform, false);
                go.transform.localPosition = new Vector3(x, y, 0f);

                var renderer = go.AddComponent<SpriteRenderer>();
                renderer.sprite = GameAssets.Chalice;
                renderer.transform.localScale = _chaliceBaseScale;
                renderer.gameObject.SetActive(false);

                _chalices[i] = renderer;
            }

            if (Box.IsDoorBroken)
            {
                _doors.gameObject.SetActive(false);
                UpdateChalicesVisibility();
            }
            else
            {
                _doors.gameObject.SetActive(true);
            }
        }

        private SpriteRenderer CreateOverlay(string name, Sprite sprite, float fill)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            FitToFootprint(renderer, sprite, fill);
            return renderer;
        }

        protected override void SetSortingOrder(int order)
        {
            base.SetSortingOrder(order);
            if (_doors != null) { _doors.sortingOrder = order + 1; }
            for (int i = 0; i < 10; i++)
            {
                if (_chalices[i] != null)
                {
                    int slotIndex = i % 5;
                    int depthOffset = slotIndex == 2 ? 2 : (slotIndex == 1 || slotIndex == 3 ? 1 : 0);
                    _chalices[i].sortingOrder = order + 1 + depthOffset;
                }
            }
        }

        public override void OnDamaged(ObstacleDamage damage)
        {
            // Use a safe tween that always resets to Vector3.one on completion to prevent scale accumulation
            // when hit by multiple damage sources in the same step.
            Transform tTarget = transform;
            Tween.To(t =>
            {
                if (tTarget == null) return;
                float wave = Mathf.Sin(t * Mathf.PI);
                tTarget.localScale = Vector3.one * (1f + 0.08f * wave);
            }, 0.2f, Ease.Linear, 0f, () =>
            {
                if (tTarget != null) tTarget.localScale = Vector3.one;
            });

            if (damage.DoorBroken)
            {
                // The door shatters and the chalices are revealed.
                Fx.Burst(CenterWorldPosition, Fx.ChaliceBoxDoorShards(), 18, 0.70f * Board.CellSize, speed: 6f);
                _doors.gameObject.SetActive(false);
                UpdateChalicesVisibility();
                for (int i = 0; i < 10; i++)
                {
                    if (_chalices[i].gameObject.activeSelf)
                    {
                        Tween.Punch(_chalices[i].transform, 0.3f, 0.3f);
                    }
                }
                return;
            }
            if (!Box.IsDoorBroken)
            {
                Fx.Burst(CenterWorldPosition, Fx.ChaliceBoxShards(), 10, 0.68f * Board.CellSize, speed: 5.5f);
                return;
            }
            // Chalice phase: every point of damage collects a chalice.
            if (damage.ChalicesCollected > 0)
            {
                UpdateChalicesVisibility();
                for (int i = 0; i < 10; i++)
                {
                    if (_chalices[i].gameObject.activeSelf)
                    {
                        Tween.Punch(_chalices[i].transform, 0.25f, 0.25f);
                    }
                }
                Fx.Burst(CenterWorldPosition, new[] { GameAssets.Chalice },
                    damage.ChalicesCollected, 0.75f * Board.CellSize, 5.5f, 8f, 0.8f);
            }
        }

        private void UpdateChalicesVisibility()
        {
            int remaining = Box.ChalicesRemaining;
            int[] activationOrder = { 4, 5, 3, 6, 2, 7, 1, 8, 0, 9 };

            for (int i = 0; i < 10; i++)
            {
                _chalices[i].gameObject.SetActive(false);
            }

            for (int i = 0; i < remaining && i < 10; i++)
            {
                int slotIndex = activationOrder[i];
                _chalices[slotIndex].gameObject.SetActive(true);
                _chalices[slotIndex].transform.localScale = _chaliceBaseScale; // Reset scale to base scale to override any punch scaling issues
            }
        }

        public override void SpawnDestructionFx()
        {
            Fx.Burst(CenterWorldPosition, Fx.ChaliceBoxShards(), 22, 0.72f * Board.CellSize, speed: 6f);
        }
    }
}
