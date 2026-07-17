using System.Collections.Generic;
using CubeBlast.Core;
using UnityEngine;
using UnityEngine.UI;

namespace CubeBlast.Game
{
    /// <summary>
    /// Top HUD of the level: remaining move count and the level goals with
    /// live counters (a check mark replaces the counter when a goal is done).
    /// </summary>
    public sealed class TopPanelView
    {
        private static readonly Vector2 GoalBoxAnchor = new Vector2(0.24f, 0.40f);
        private static readonly Vector2 MoveBoxAnchor = new Vector2(0.76f, 0.40f);
        private static readonly Vector2 GoalTabAnchor = new Vector2(0.24f, 0.83f);
        private static readonly Vector2 MoveTabAnchor = new Vector2(0.76f, 0.83f);
        private static readonly Color PanelMiddleColor = new Color(0.34f, 0f, 0.74f);
        private static readonly Color PanelTopColor = new Color(0.27f, 0.12f, 0.57f);

        private sealed class GoalEntry
        {
            public Text Counter;
            public Image Check;
            public Transform Root;
            public int LastValue = int.MinValue;
        }

        private Text _movesText;
        private readonly Dictionary<string, GoalEntry> _goals = new Dictionary<string, GoalEntry>();

        public void Build(Transform canvas, int levelNumber, int moves, List<GoalCount> goals)
        {
            var panel = UiFactory.CreateImage("TopPanel", canvas, GameAssets.TopPanel);
            panel.type = Image.Type.Simple;
            panel.color = Color.clear;
            var rect = panel.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(1080f, 430f);
            panel.raycastTarget = false;
            RebuildInsetHudArt(panel.transform);

            var goalLabel = UiFactory.CreateText("GoalLabel", panel.transform, "Goal", 54, Color.white);
            var goalLabelRect = goalLabel.rectTransform;
            goalLabelRect.anchorMin = goalLabelRect.anchorMax = GoalTabAnchor;
            goalLabelRect.anchoredPosition = new Vector2(0f, -15f);
            goalLabelRect.sizeDelta = new Vector2(220f, 72f);

            var movesLabel = UiFactory.CreateText("MoveLabel", panel.transform, "Move", 54, Color.white);
            var movesLabelRect = movesLabel.rectTransform;
            movesLabelRect.anchorMin = movesLabelRect.anchorMax = MoveTabAnchor;
            movesLabelRect.anchoredPosition = new Vector2(0f, -15f);
            movesLabelRect.sizeDelta = new Vector2(220f, 72f);

            _movesText = UiFactory.CreateText("Moves", panel.transform, moves.ToString(), 100, Color.white);
            var movesOutline = _movesText.GetComponent<Outline>();
            if (movesOutline != null)
            {
                movesOutline.effectColor = Color.black;
                movesOutline.effectDistance = new Vector2(4f, -4f);
            }
            var movesRect = _movesText.rectTransform;
            movesRect.anchorMin = movesRect.anchorMax = MoveBoxAnchor;
            movesRect.anchoredPosition = new Vector2(0f, 10f);
            movesRect.sizeDelta = new Vector2(190f, 150f);

            List<List<GoalCount>> rows = new List<List<GoalCount>>();
            if (goals.Count <= 2)
            {
                rows.Add(goals);
            }
            else if (goals.Count == 3)
            {
                rows.Add(new List<GoalCount> { goals[0], goals[1] });
                rows.Add(new List<GoalCount> { goals[2] });
            }
            else if (goals.Count == 4)
            {
                rows.Add(new List<GoalCount> { goals[0], goals[1] });
                rows.Add(new List<GoalCount> { goals[2], goals[3] });
            }
            else
            {
                int mid = (goals.Count + 1) / 2;
                rows.Add(goals.GetRange(0, mid));
                rows.Add(goals.GetRange(mid, goals.Count - mid));
            }

            float scale = 1f;
            if (rows.Count > 1)
            {
                scale = 0.65f;
            }
            else if (goals.Count == 2)
            {
                scale = 0.78f;
            }

            for (int r = 0; r < rows.Count; r++)
            {
                var rowItems = rows[r];
                float y = GoalBoxAnchor.y + (rows.Count > 1 ? (r == 0 ? 0.06f : -0.10f) : 0f);
                
                float spacingX = 0.08f;
                if (rows.Count == 1)
                {
                    spacingX = goals.Count == 2 ? 0.085f : 0.11f;
                }
                
                // Shift layout slightly to the left to compensate for the right-hand counter offset
                float startX = (GoalBoxAnchor.x - 0.008f * scale) - spacingX * 0.5f * (rowItems.Count - 1);
                for (int c = 0; c < rowItems.Count; c++)
                {
                    CreateGoalEntry(panel.transform, rowItems[c], new Vector2(startX + spacingX * c, y), scale);
                }
            }
            UpdateGoals(goals);
        }

        private static void RebuildInsetHudArt(Transform parent)
        {
            var topPanel = GameAssets.TopPanel;
            if (topPanel == null || topPanel.texture == null)
            {
                return;
            }

            CreateSolidPatch(parent, "HudTopBand", new Vector2(0.5f, 0.86f), new Vector2(1080f, 120f), PanelTopColor);
            CreateSolidPatch(parent, "HudMiddleBand", new Vector2(0.5f, 0.44f), new Vector2(1080f, 270f), PanelMiddleColor);
            CreateSolidPatch(parent, "HudTopDivider", new Vector2(0.5f, 0.70f), new Vector2(1080f, 16f), new Color(0.25f, 0f, 0.58f));
            CreateSolidPatch(parent, "HudBottomShadow", new Vector2(0.5f, 0.055f), new Vector2(1080f, 18f), new Color(0.55f, 0.24f, 0.03f));
            CreateSolidPatch(parent, "HudBottomStripe", new Vector2(0.5f, 0.09f), new Vector2(1080f, 28f), new Color(0.98f, 0.58f, 0.02f));

            var tabSprite = CreateTopPanelPart("HudTab", topPanel, new Rect(16f, 472f, 330f, 150f), 100f);
            var boxSprite = CreateTopPanelPart("HudBox", topPanel, new Rect(36f, 142f, 292f, 286f), 100f);

            CreateHudPart(parent, "GoalTabArt", tabSprite, GoalTabAnchor, new Vector2(270f, 112f), false);
            CreateHudPart(parent, "MoveTabArt", tabSprite, MoveTabAnchor, new Vector2(270f, 112f), true);
            CreateHudPart(parent, "GoalBoxArt", boxSprite, GoalBoxAnchor, new Vector2(222f, 222f), false);
            CreateHudPart(parent, "MoveBoxArt", boxSprite, MoveBoxAnchor, new Vector2(222f, 222f), false);
        }

        private static void CreateSolidPatch(Transform parent, string name, Vector2 anchor, Vector2 size, Color color)
        {
            var rect = UiFactory.CreateRect(name, parent);
            rect.anchorMin = rect.anchorMax = anchor;
            rect.sizeDelta = size;
            var image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
        }

        private static Sprite CreateTopPanelPart(string name, Sprite source, Rect rect, float pixelsPerUnit)
        {
            return Sprite.Create(source.texture, rect, new Vector2(0.5f, 0.5f), pixelsPerUnit, 0, SpriteMeshType.FullRect);
        }

        private static void CreateHudPart(Transform parent, string name, Sprite sprite, Vector2 anchor, Vector2 size, bool mirror)
        {
            var image = UiFactory.CreateImage(name, parent, sprite);
            image.type = Image.Type.Simple;
            image.raycastTarget = false;
            var rect = image.rectTransform;
            rect.anchorMin = rect.anchorMax = anchor;
            rect.sizeDelta = size;
            rect.localScale = mirror ? new Vector3(-1f, 1f, 1f) : Vector3.one;
        }

        private void CreateGoalEntry(Transform parent, GoalCount goal, Vector2 anchor, float scale = 1f)
        {
            var entry = new GoalEntry();
            var root = UiFactory.CreateRect("Goal_" + goal.Key, parent);
            root.anchorMin = root.anchorMax = anchor;
            root.sizeDelta = new Vector2(170f, 170f);
            root.localScale = new Vector3(scale, scale, 1f);
            entry.Root = root;

            var icon = UiFactory.CreateImage("Icon", root, GameAssets.GoalIcon(goal.Key));
            var iconRect = icon.rectTransform;
            iconRect.anchorMin = iconRect.anchorMax = new Vector2(0.5f, 0.5f);
            iconRect.anchoredPosition = Vector2.zero;
            iconRect.sizeDelta = new Vector2(94f, 94f);
            icon.preserveAspect = true;
            icon.raycastTarget = false;

             entry.Counter = UiFactory.CreateText("Count", root, goal.Remaining.ToString(), 38, Color.white);
             entry.Counter.fontStyle = FontStyle.Bold;
             var counterOutline = entry.Counter.GetComponent<Outline>();
             if (counterOutline != null)
             {
                 counterOutline.effectColor = Color.black;
                 counterOutline.effectDistance = new Vector2(3f, -3f);
             }
             var counterRect = entry.Counter.rectTransform;
             counterRect.anchorMin = counterRect.anchorMax = new Vector2(0.5f, 0.5f);
             counterRect.anchoredPosition = new Vector2(24f, -26f);
             counterRect.sizeDelta = new Vector2(78f, 50f);
             entry.Counter.alignment = TextAnchor.LowerRight;
 
             entry.Check = UiFactory.CreateImage("Check", root, GameAssets.GoalCheck);
             var checkRect = entry.Check.rectTransform;
             checkRect.anchorMin = checkRect.anchorMax = new Vector2(0.5f, 0.5f);
             checkRect.anchoredPosition = new Vector2(24f, -26f);
             checkRect.sizeDelta = new Vector2(34f, 34f);
             entry.Check.preserveAspect = true;
             entry.Check.raycastTarget = false;
             entry.Check.gameObject.SetActive(false);

            _goals[goal.Key] = entry;
        }

        public void UpdateMoves(int moves)
        {
            if (_movesText == null)
            {
                return;
            }
            _movesText.text = Mathf.Max(0, moves).ToString();
            Tween.Punch(_movesText.transform, 0.2f, 0.18f);
        }

        public void UpdateGoals(List<GoalCount> goals)
        {
            // Obstacles that no longer appear in the list are fully cleared.
            var seen = new HashSet<string>();
            for (int i = 0; i < goals.Count; i++)
            {
                seen.Add(goals[i].Key);
                GoalEntry entry;
                if (!_goals.TryGetValue(goals[i].Key, out entry))
                {
                    continue;
                }
                SetGoalValue(entry, goals[i].Remaining);
            }
            foreach (var pair in _goals)
            {
                if (!seen.Contains(pair.Key))
                {
                    SetGoalValue(pair.Value, 0);
                }
            }
        }

        private static void SetGoalValue(GoalEntry entry, int value)
        {
            if (entry.LastValue == value)
            {
                return;
            }
            bool changed = entry.LastValue != int.MinValue;
            entry.LastValue = value;
            bool done = value <= 0;
            entry.Counter.gameObject.SetActive(!done);
            entry.Check.gameObject.SetActive(done);
            entry.Counter.text = value.ToString();
            if (changed)
            {
                Tween.Punch(done ? entry.Check.transform : entry.Counter.transform, 0.3f, 0.22f);
            }
        }
    }
}
