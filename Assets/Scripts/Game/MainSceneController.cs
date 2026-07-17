using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CubeBlast.Game
{
    /// <summary>
    /// Entry point of the MainScene. Shows the area background and the
    /// LevelButton with the player's current level (or a finished text once
    /// all levels are completed).
    /// </summary>
    public sealed class MainSceneController : MonoBehaviour
    {
        public const string LevelSceneName = "LevelScene";

        private void Start()
        {
            UiFactory.CreateCamera(new Color(0.13f, 0.36f, 0.58f));
            UiFactory.EnsureEventSystem();

            var canvas = UiFactory.CreateCanvas("MainCanvas", 0);
            UiFactory.CreateFullScreenBackground(canvas.transform, GameAssets.MenuBackground);
            CreateLevelButton(canvas.transform);
        }

        private void CreateLevelButton(Transform parent)
        {
            bool finished = LevelProgress.AllLevelsFinished;

            // Transparent hit area - children handle all visuals so we can control draw order.
            var button = UiFactory.CreateButton("LevelButton", parent, null, OnLevelButtonTapped);
            button.image.color = Color.clear;
            var rect = (RectTransform)button.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.12f);
            rect.sizeDelta = new Vector2(540f, 180f);

            // The menu button assets are left-half sprites. Build the full button
            // by mirroring the same sprite on the right side.
            CreateMirroredButtonLayer("Frame", button.transform, GameAssets.MenuButtonFrame, Vector2.zero, Vector2.zero, Color.white);

            // 2nd child: green button face inset so the golden border is visible.
            var faceColor = Color.white;
            CreateMirroredButtonLayer("Face", button.transform, GameAssets.MenuButton, new Vector2(16f, 14f), new Vector2(-16f, -14f), faceColor);

            // 3rd child (top layer): label text.
            string label = finished ? "Finished" : "Level " + LevelProgress.CurrentLevel;
            var text = UiFactory.CreateText("Label", button.transform, label, 64, Color.white);
            UiFactory.Stretch(text.rectTransform);

            if (!finished)
            {
                PulseForever(button.transform);
            }

            // Small entrance animation.
            button.transform.localScale = Vector3.zero;
            Tween.Scale(button.transform, Vector3.one, 0.4f, Ease.OutBack, 0.1f);
        }

        private void CreateMirroredButtonLayer(string name, Transform parent, Sprite sprite, Vector2 offsetMin, Vector2 offsetMax, Color color)
        {
            var layer = UiFactory.CreateRect(name, parent);
            UiFactory.Stretch(layer);
            layer.offsetMin = offsetMin;
            layer.offsetMax = offsetMax;

            CreateButtonHalf(name + "Left", layer, sprite, false, color);
            CreateButtonHalf(name + "Right", layer, sprite, true, color);
        }

        private void CreateButtonHalf(string name, Transform parent, Sprite sprite, bool mirror, Color color)
        {
            var image = UiFactory.CreateImage(name, parent, sprite);
            image.type = Image.Type.Sliced; // Force Sliced to preserve corner shapes and keep top/bottom edges flat/straight
            var rect = image.rectTransform;
            rect.anchorMin = mirror ? new Vector2(0.5f, 0f) : Vector2.zero;
            rect.anchorMax = mirror ? Vector2.one : new Vector2(0.5f, 1f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localScale = mirror ? new Vector3(-1f, 1f, 1f) : Vector3.one;
            image.color = color;
            image.raycastTarget = false;
        }

        private void PulseForever(Transform target)
        {
            if (target == null) { return; }
            Tween.To(t =>
            {
                if (target == null) { return; }
                float wave = Mathf.Sin(t * Mathf.PI);
                target.localScale = Vector3.one * (1f + 0.035f * wave);
            }, 1.6f, Ease.InOutQuad, 0.6f, () => PulseForever(target));
        }

        private void OnLevelButtonTapped()
        {
            if (LevelProgress.AllLevelsFinished)
            {
                return;
            }
            SceneManager.LoadScene(LevelSceneName);
        }
    }
}
