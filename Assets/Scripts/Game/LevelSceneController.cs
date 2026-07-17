using System.Collections;
using System.Collections.Generic;
using CubeBlast.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CubeBlast.Game
{
    /// <summary>
    /// Entry point of the LevelScene. Creates the simulation for the current
    /// level, feeds player taps into it and animates the resulting turn steps.
    /// The simulation itself lives in CubeBlast.Core and never touches Unity.
    /// </summary>
    public sealed class LevelSceneController : MonoBehaviour
    {
        private const float CellSize = 1f;
        private const float BoardViewportWidth = 0.82f;
        private const float RocketCellsPerSecond = 16f;
        private const float ExplosionWaveDelay = 0.16f;
        private const float FallSecondsPerCell = 0.055f;

        private BoardEngine _engine;
        private BoardView _boardView;
        private TopPanelView _topPanel;
        private Canvas _canvas;
        private Camera _camera;
        private SpriteRenderer _background;
        private int _levelNumber;
        private bool _busy;
        private bool _levelOver;
        private int _runningExplosions;

        private void Start()
        {
            _levelNumber = Mathf.Clamp(LevelProgress.CurrentLevel, LevelProgress.FirstLevel, LevelProgress.MaxLevel);
            var definition = LevelRepository.Load(_levelNumber);
            if (definition == null)
            {
                SceneManager.LoadScene("MainScene");
                return;
            }

            _engine = new BoardEngine(definition, new SystemRandomProvider());

            _camera = UiFactory.CreateCamera(new Color(0.09f, 0.28f, 0.47f));
            CreateGameplayBackground();
            UiFactory.EnsureEventSystem();

            var boardGo = new GameObject("Board");
            _boardView = boardGo.AddComponent<BoardView>();
            _boardView.Build(_engine.Board, CellSize);
            FitCamera();

            _canvas = UiFactory.CreateCanvas("LevelCanvas", 10);
            _topPanel = new TopPanelView();
            _topPanel.Build(_canvas.transform, _levelNumber, _engine.MovesLeft, _engine.GetGoals());

            ApplyHints();
            StartCoroutine(PlayIntro());
        }

        private void CreateGameplayBackground()
        {
            var go = new GameObject("GameplayBackground");
            _background = go.AddComponent<SpriteRenderer>();
            _background.sprite = GameAssets.MenuBackground;
            _background.sortingOrder = -100;
        }

        /// <summary>Portrait-friendly framing: keep the board in the playable area under the HUD.</summary>
        private void FitCamera()
        {
            float aspect = (float)Screen.width / Screen.height;
            float size = _boardView.BoardWorldWidth / (2f * aspect * BoardViewportWidth);
            size = Mathf.Max(size, _boardView.BoardWorldHeight * 0.5f + 0.25f);
            _camera.orthographicSize = size;

            // Center the board vertically in the playable area beneath the top HUD (Y center at -0.224f * size)
            float boardCenterY = _camera.transform.position.y - 0.224f * size;
            _boardView.transform.position = new Vector3(0f, boardCenterY, 0f);

            if (_background != null && _background.sprite != null)
            {
                _background.transform.position = new Vector3(0f, 0f, 1f);
                var bounds = _background.sprite.bounds.size;
                float worldHeight = size * 2f;
                float worldWidth = worldHeight * aspect;
                float scale = Mathf.Max(worldWidth / bounds.x, worldHeight / bounds.y);
                _background.transform.localScale = new Vector3(scale, scale, 1f);
            }
        }

        private void LateUpdate()
        {
            if (_camera != null && _boardView != null)
            {
                FitCamera(); // keeps the framing correct when the editor game view is resized
            }
        }

        // ------------------------------------------------------------- input

        private void Update()
        {
            if (_busy || _levelOver || _engine == null || !Input.GetMouseButtonDown(0))
            {
                return;
            }
            var world = _camera.ScreenToWorldPoint(Input.mousePosition);
            GridPosition cell;
            if (!_boardView.TryWorldToCell(world, out cell))
            {
                return;
            }

            var result = _engine.Tap(cell);
            if (!result.IsValidMove)
            {
                if (result.RejectedItem != null)
                {
                    var view = _boardView.GetView(result.RejectedItem);
                    if (view != null)
                    {
                        view.Wiggle();
                    }
                }
                return;
            }
            StartCoroutine(PlayTurn(result));
        }

        // ------------------------------------------------------------ intro

        private IEnumerator PlayIntro()
        {
            _busy = true;
            foreach (var view in _boardView.Views)
            {
                // Scale the root so multi-part views (Chalice Box) pop in as one piece.
                view.transform.localScale = Vector3.zero;
                float delay = 0.02f * (view.Item.Origin.X + view.Item.Origin.Y);
                Tween.Scale(view.transform, Vector3.one, 0.3f, Ease.OutBack, delay);
            }
            yield return new WaitForSeconds(0.75f);
            _busy = false;
        }

        // ------------------------------------------------------- turn playback

        private IEnumerator PlayTurn(TurnResult result)
        {
            _busy = true;
            _topPanel.UpdateMoves(result.MovesLeft);

            int index = 0;
            var steps = result.Steps;
            while (index < steps.Count)
            {
                var explosion = steps[index] as ExplosionStep;
                if (explosion != null)
                {
                    // Play consecutive explosions together, staggered by chain wave.
                    var batch = new List<ExplosionStep>();
                    while (index < steps.Count && steps[index] is ExplosionStep)
                    {
                        batch.Add((ExplosionStep)steps[index]);
                        index++;
                    }
                    yield return PlayExplosionBatch(batch);
                    continue;
                }

                var step = steps[index];
                index++;
                if (step is BlastStep) { yield return PlayBlast((BlastStep)step); }
                else if (step is ComboGatherStep) { yield return PlayGather((ComboGatherStep)step); }
                else if (step is GravityStep) { yield return PlayGravity((GravityStep)step); }
                else if (step is ShuffleStep) { yield return PlayShuffle((ShuffleStep)step); }
            }

            ApplyHints();
            _topPanel.UpdateGoals(_engine.GetGoals());

            if (result.State == LevelState.Won)
            {
                _levelOver = true;
                yield return new WaitForSeconds(0.35f);
                LevelProgress.AdvanceAfterWin();
                yield return CelebrationView.Play(_canvas.transform, this);
                SceneManager.LoadScene("MainScene");
                yield break;
            }
            if (result.State == LevelState.Failed)
            {
                _levelOver = true;
                yield return new WaitForSeconds(0.45f);
                FailPopupView.Show(_canvas.transform, _levelNumber);
                yield break;
            }
            _busy = false;
        }

        // ------------------------------------------------------------- blasts

        private IEnumerator PlayBlast(BlastStep step)
        {
            if (step.CreatedSpecial != null)
            {
                // The group flies into the tapped cell and merges into the special item.
                var center = _boardView.CellToWorld(step.TapPosition);
                foreach (var cube in step.Cubes)
                {
                    var view = _boardView.GetView(cube);
                    if (view == null) { continue; }
                    view.UpdateSorting();
                    Tween.To(MakeMoveLerp(view.transform, center), 0.22f, Ease.InQuad);
                    Tween.Scale(view.Body, view.Body.localScale * 0.55f, 0.22f, Ease.InQuad);
                }
                yield return new WaitForSeconds(0.22f);
                foreach (var cube in step.Cubes)
                {
                    _boardView.RemoveView(cube, false);
                }
                Fx.Burst(center, new[] { GameAssets.RocketStar }, 7, 0.24f * CellSize, 4.5f, 3f, 0.45f);

                var specialView = _boardView.CreateView(step.CreatedSpecial);
                // The special is created at the tap cell; gravity may move it later.
                specialView.transform.position = center;
                var targetScale = specialView.Body.localScale;
                specialView.Body.localScale = Vector3.zero;
                Tween.Scale(specialView.Body, targetScale, 0.28f, Ease.OutBack);
            }
            else
            {
                foreach (var cube in step.Cubes)
                {
                    _boardView.RemoveView(cube, true);
                }
            }

            ApplyObstacleDamages(step.Damages);
            yield return new WaitForSeconds(0.1f);
        }

        // -------------------------------------------------------- explosions

        private IEnumerator PlayGather(ComboGatherStep step)
        {
            var center = _boardView.CellToWorld(step.Center);
            foreach (var item in step.Items)
            {
                var view = _boardView.GetView(item);
                if (view == null) { continue; }
                Tween.To(MakeMoveLerp(view.transform, center), 0.2f, Ease.InBack);
            }
            yield return new WaitForSeconds(0.21f);
            foreach (var item in step.Items)
            {
                _boardView.RemoveView(item, false);
            }
            Fx.Burst(center, new[] { GameAssets.RocketStar }, 10, 0.28f * CellSize, 5f, 3f, 0.5f);
            Fx.ShakeCamera(0.18f, 0.25f);
            yield return new WaitForSeconds(0.05f);
        }

        private IEnumerator PlayExplosionBatch(List<ExplosionStep> batch)
        {
            _runningExplosions = 0;
            foreach (var step in batch)
            {
                _runningExplosions++;
                StartCoroutine(PlayExplosion(step, step.Wave * ExplosionWaveDelay));
            }
            while (_runningExplosions > 0)
            {
                yield return null;
            }
        }

        private IEnumerator PlayExplosion(ExplosionStep step, float delay)
        {
            if (delay > 0f)
            {
                yield return new WaitForSeconds(delay);
            }

            ItemView sourceView = null;
            if (step.Source != null)
            {
                sourceView = _boardView.GetView(step.Source);
            }

            if (step.Kind == ExplosionKind.Rocket)
            {
                if (step.Source != null)
                {
                    _boardView.RemoveView(step.Source, false);
                }
                yield return PlayRocket(step);
            }
            else
            {
                yield return PlayTnt(step, sourceView);
            }
            _runningExplosions--;
        }

        private IEnumerator PlayRocket(ExplosionStep step)
        {
            var centerWorld = _boardView.CellToWorld(step.Center);
            Fx.ShakeCamera(0.08f, 0.22f);

            // Longest distance a part must travel (to the board edge, in cells).
            float maxDistance = 1f;
            foreach (var cell in step.AffectedCells)
            {
                maxDistance = Mathf.Max(maxDistance, RocketDistance(step, cell));
            }
            float duration = (maxDistance + 1f) / RocketCellsPerSecond;

            CreateRocketPart(step, centerWorld, true, maxDistance + 1f, duration);
            CreateRocketPart(step, centerWorld, false, maxDistance + 1f, duration);

            // Damage cells one by one as the parts pass over them.
            foreach (var cell in step.AffectedCells)
            {
                float time = RocketDistance(step, cell) / RocketCellsPerSecond;
                ScheduleCellHit(step, cell, time);
            }
            yield return new WaitForSeconds(duration + 0.1f);
        }

        private float RocketDistance(ExplosionStep step, GridPosition cell)
        {
            return step.Orientation == RocketOrientation.Horizontal
                ? Mathf.Abs(cell.X - step.Center.X)
                : Mathf.Abs(cell.Y - step.Center.Y);
        }

        private void CreateRocketPart(ExplosionStep step, Vector3 origin, bool positive, float cells, float duration)
        {
            var go = new GameObject("RocketPart");
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = GameAssets.RocketPart(step.Orientation, positive);
            renderer.sortingOrder = 210;
            go.transform.position = origin;
            if (renderer.sprite != null)
            {
                float scale = 0.95f * CellSize / Mathf.Max(renderer.sprite.bounds.size.x, renderer.sprite.bounds.size.y);
                go.transform.localScale = new Vector3(scale, scale, 1f);
            }

            Vector3 direction = step.Orientation == RocketOrientation.Horizontal
                ? (positive ? Vector3.right : Vector3.left)
                : (positive ? Vector3.up : Vector3.down);
            Vector3 target = origin + direction * cells * CellSize;

            var trail = StartCoroutine(EmitSmoke(go.transform, duration));
            Tween.Move(go.transform, target, duration, Ease.Linear, 0f, () =>
            {
                if (trail != null) { StopCoroutine(trail); }
                if (go != null) { Destroy(go); }
            });
        }

        private IEnumerator EmitSmoke(Transform part, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration && part != null)
            {
                Fx.Burst(part.position, new[] { GameAssets.RocketSmoke }, 1, 0.22f * CellSize, 0.6f, -0.5f, 0.4f);
                yield return new WaitForSeconds(0.035f);
                elapsed += 0.035f;
            }
        }

        private IEnumerator PlayTnt(ExplosionStep step, ItemView sourceView)
        {
            var centerWorld = _boardView.CellToWorld(step.Center);
            float radius = step.Kind == ExplosionKind.TntTnt ? 3.5f : 2.5f;

            if (sourceView != null)
            {
                Vector3 originalPos = sourceView.transform.position;
                Vector3 baseScale = sourceView.Body != null ? sourceView.Body.localScale : Vector3.one;
                float duration = 0.26f;
                
                Tween.To(t =>
                {
                    if (sourceView == null) return;
                    if (sourceView.Body != null)
                    {
                        float scaleFactor = 1f + Mathf.Sin(t * Mathf.PI) * 0.35f;
                        sourceView.Body.localScale = baseScale * scaleFactor;
                    }
                    float wiggleAmount = Mathf.Sin(t * Mathf.PI * 4f) * 0.12f * (1f - t);
                    sourceView.transform.position = originalPos + (Vector3)(Random.insideUnitCircle.normalized * wiggleAmount);
                }, duration, Ease.Linear);
                
                yield return new WaitForSeconds(duration);
                
                if (step.Source != null)
                {
                    _boardView.RemoveView(step.Source, false);
                }
            }

            CreateExplosionFlash(centerWorld, radius);

            Fx.ShakeCamera(step.Kind == ExplosionKind.TntTnt ? 0.3f : 0.18f, 0.35f);
            Fx.Burst(centerWorld, Fx.TntShards(), step.Kind == ExplosionKind.TntTnt ? 34 : 22,
                0.4f * CellSize, radius * 2.6f, 7f, 0.6f);
            Fx.Burst(centerWorld, new[] { GameAssets.RocketStar }, 10, 0.3f * CellSize, radius * 2f, 4f, 0.5f);

            float maxRing = 0f;
            foreach (var cell in step.AffectedCells)
            {
                float ring = Mathf.Max(Mathf.Abs(cell.X - step.Center.X), Mathf.Abs(cell.Y - step.Center.Y));
                maxRing = Mathf.Max(maxRing, ring);
                ScheduleCellHit(step, cell, 0.03f + ring * 0.04f);
            }
            yield return new WaitForSeconds(0.1f + maxRing * 0.04f + 0.1f);
        }

        /// <summary>Plays the destruction/damage visuals of one exploded cell at the right time.</summary>
        private void ScheduleCellHit(ExplosionStep step, GridPosition cell, float time)
        {
            Tween.Delay(time, () =>
            {
                foreach (var cube in step.DestroyedCubes)
                {
                    if (cube.Origin == cell)
                    {
                        _boardView.RemoveView(cube, true);
                    }
                }
                foreach (var special in step.TriggeredSpecials)
                {
                    if (special.Origin == cell)
                    {
                        _boardView.RemoveView(special, true);
                    }
                }
                for (int i = 0; i < step.Damages.Count; i++)
                {
                    var damage = step.Damages[i];
                    if (damage.Obstacle.Occupies(cell))
                    {
                        ApplySingleObstacleDamage(damage);
                        step.Damages.RemoveAt(i);
                        break;
                    }
                }
            });
        }

        // ------------------------------------------------------------ damage

        private void ApplyObstacleDamages(List<ObstacleDamage> damages)
        {
            foreach (var damage in damages)
            {
                ApplySingleObstacleDamage(damage);
            }
        }

        private void ApplySingleObstacleDamage(ObstacleDamage damage)
        {
            var view = _boardView.GetView(damage.Obstacle);
            if (view == null)
            {
                return;
            }
            view.OnDamaged(damage);
            if (damage.Destroyed)
            {
                _boardView.RemoveView(damage.Obstacle, true);
            }
            _topPanel.UpdateGoals(_engine.GetGoals());
        }

        // ----------------------------------------------------------- gravity

        private IEnumerator PlayGravity(GravityStep step)
        {
            float longest = 0f;
            foreach (var fall in step.Falls)
            {
                var view = _boardView.GetView(fall.Item);
                if (view == null) { continue; }
                longest = Mathf.Max(longest, AnimateFall(view, fall.From, fall.To));
            }
            foreach (var spawn in step.Spawns)
            {
                var view = _boardView.CreateView(spawn.Cube);
                var from = new GridPosition(spawn.Target.X, spawn.SpawnRow);
                longest = Mathf.Max(longest, AnimateFall(view, from, spawn.Target));
            }
            if (longest > 0f)
            {
                yield return new WaitForSeconds(longest + 0.06f);
            }
        }

        /// <summary>Coded fall: accelerates downwards, then lands with a small squash.</summary>
        private float AnimateFall(ItemView view, GridPosition from, GridPosition to)
        {
            float distance = Mathf.Abs(from.Y - to.Y);
            float duration = 0.06f + distance * FallSecondsPerCell;

            view.transform.position = _boardView.CellToWorld(from);
            view.UpdateSorting();
            var target = _boardView.CellToWorld(to);
            Tween.To(MakeMoveLerp(view.transform, target), duration, Ease.InQuad, 0f, () =>
            {
                if (view == null) { return; }
                view.SyncPosition();
                var body = view.Body;
                var baseScale = body.localScale;
                Tween.To(t =>
                {
                    if (body == null) { return; }
                    // Damped springy oscillation for landing juice: squashes (wider/flatter), rebounds, and settles.
                    float wave = Mathf.Sin(t * Mathf.PI * 2f) * (1f - t);
                    body.localScale = new Vector3(
                        baseScale.x * (1f + 0.12f * wave),
                        baseScale.y * (1f - 0.16f * wave),
                        1f);
                }, 0.22f, Ease.Linear, 0f, () =>
                {
                    if (body != null) { body.localScale = baseScale; }
                });
            });
            return duration;
        }

        private IEnumerator PlayShuffle(ShuffleStep step)
        {
            yield return new WaitForSeconds(0.15f);
            foreach (var move in step.Moves)
            {
                var view = _boardView.GetView(move.Item);
                if (view == null) { continue; }
                var target = _boardView.CellToWorld(move.To);
                Tween.To(MakeMoveLerp(view.transform, target), 0.35f, Ease.InOutQuad);
            }
            yield return new WaitForSeconds(0.4f);
            foreach (var move in step.Moves)
            {
                var view = _boardView.GetView(move.Item);
                if (view != null)
                {
                    view.SyncPosition();
                    view.UpdateSorting();
                }
            }
        }

        // ------------------------------------------------------------ helpers

        /// <summary>World-space move lerp that captures the start position on first update.</summary>
        private static System.Action<float> MakeMoveLerp(Transform target, Vector3 to)
        {
            Vector3 from = Vector3.zero;
            bool captured = false;
            return t =>
            {
                if (target == null) { return; }
                if (!captured) { from = target.position; captured = true; }
                target.position = Vector3.LerpUnclamped(from, to, t);
            };
        }

        private void ApplyHints()
        {
            var hints = _engine.GetSpecialHints();
            foreach (var view in _boardView.Views)
            {
                var cubeView = view as CubeItemView;
                if (cubeView == null)
                {
                    continue;
                }
                SpecialHint hint;
                hints.TryGetValue(cubeView.Item.Id, out hint);
                cubeView.ApplyHint(hint);
            }
        }

        private void CreateExplosionFlash(Vector3 position, float radius)
        {
            var go = new GameObject("~flash");
            go.transform.position = position;
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = GameAssets.CelebrationStarParticle;
            renderer.sortingOrder = 220;
            renderer.color = new Color(1f, 1f, 0.9f, 0.8f);

            Tween.To(t =>
            {
                if (go == null) return;
                // Starts at 60% of the explosion diameter and expands to 125% of the diameter,
                // covering the entire affected area (5x5 or 7x7) dynamically as it fades.
                float diameter = radius * CellSize * 2f;
                float scale = diameter * Mathf.Lerp(0.6f, 1.25f, t);
                go.transform.localScale = new Vector3(scale, scale, 1f);
                
                var c = renderer.color;
                c.a = Mathf.Lerp(0.9f, 0f, t);
                renderer.color = c;
            }, 0.28f, Ease.OutQuad, 0f, () =>
            {
                if (go != null) Destroy(go);
            });
        }
    }
}
