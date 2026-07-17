using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CubeBlast.Game
{
    /// <summary>
    /// Popup shown when the player runs out of moves: a close button returns to
    /// the MainScene and a try-again button replays the level.
    /// </summary>
    public static class FailPopupView
    {
        public static void Show(Transform canvas, int levelNumber)
        {
            // Dim layer that also blocks input to the board UI.
            var dim = UiFactory.CreateImage("FailDim", canvas, null);
            dim.sprite = null;
            dim.color = new Color(0f, 0f, 0f, 0f);
            UiFactory.Stretch(dim.rectTransform);
            Tween.To(t =>
            {
                if (dim != null) { dim.color = new Color(0f, 0f, 0f, 0.65f * t); }
            }, 0.25f);

            var popup = UiFactory.CreateImage("FailPopup", dim.transform, GameAssets.PopupBase);
            var rect = popup.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.52f);
            rect.sizeDelta = new Vector2(720f, 900f);

            var ribbon = UiFactory.CreateImage("Ribbon", popup.transform, GameAssets.PopupRibbon);
            var ribbonRect = ribbon.rectTransform;
            ribbonRect.anchorMin = ribbonRect.anchorMax = new Vector2(0.5f, 1f);
            ribbonRect.anchoredPosition = new Vector2(0f, 30f);
            ribbonRect.sizeDelta = new Vector2(600f, 160f);
            ribbon.raycastTarget = false;

            var ribbonText = UiFactory.CreateText("RibbonLabel", ribbon.transform, "Level " + levelNumber, 52, Color.white);
            UiFactory.Stretch(ribbonText.rectTransform);
            ribbonText.rectTransform.offsetMax = new Vector2(0f, -18f);

            var title = UiFactory.CreateText("Title", popup.transform, "Level Failed!", 64, new Color(1f, 0.9f, 0.25f));
            var titleRect = title.rectTransform;
            titleRect.anchorMin = titleRect.anchorMax = new Vector2(0.5f, 0.66f);
            titleRect.anchoredPosition = Vector2.zero;

            var message = UiFactory.CreateText("Message", popup.transform,
                "You ran out of moves before\nclearing all the obstacles.", 38, Color.white);
            var messageRect = message.rectTransform;
            messageRect.anchorMin = messageRect.anchorMax = new Vector2(0.5f, 0.48f);
            messageRect.anchoredPosition = Vector2.zero;

            // Close: back to the MainScene.
            var close = UiFactory.CreateButton("CloseButton", popup.transform, GameAssets.CloseButton,
                () => SceneManager.LoadScene("MainScene"));
            var closeRect = (RectTransform)close.transform;
            closeRect.anchorMin = closeRect.anchorMax = new Vector2(1f, 1f);
            closeRect.anchoredPosition = new Vector2(-30f, -10f);
            closeRect.sizeDelta = new Vector2(96f, 96f);
            close.image.preserveAspect = true;

            // Try again: replay the current level.
            // Transparent hit area; children control draw order (same pattern as MainSceneController).
            var tryAgain = UiFactory.CreateButton("TryAgainButton", popup.transform, null,
                () => SceneManager.LoadScene("LevelScene"));
            tryAgain.image.color = Color.clear;
            var tryRect = (RectTransform)tryAgain.transform;
            tryRect.anchorMin = tryRect.anchorMax = new Vector2(0.5f, 0.2f);
            tryRect.sizeDelta = new Vector2(540f, 180f);

            // 1st child (bottom): golden frame mirrored.
            CreateMirroredButtonLayer("Frame", tryAgain.transform, GameAssets.MenuButtonFrame, Vector2.zero, Vector2.zero, Color.white);

            // 2nd child: green button face inset so the golden border is visible (mirrored).
            CreateMirroredButtonLayer("Face", tryAgain.transform, GameAssets.MenuButton, new Vector2(16f, 14f), new Vector2(-16f, -14f), Color.white);

            // 3rd child (top): label text.
            var tryText = UiFactory.CreateText("Label", tryAgain.transform, "Try Again", 64, Color.white);
            UiFactory.Stretch(tryText.rectTransform);

            popup.transform.localScale = Vector3.zero;
            Tween.Scale(popup.transform, Vector3.one, 0.35f, Ease.OutBack, 0.1f);
        }

        private static void CreateMirroredButtonLayer(string name, Transform parent, Sprite sprite, Vector2 offsetMin, Vector2 offsetMax, Color color)
        {
            var layer = UiFactory.CreateRect(name, parent);
            UiFactory.Stretch(layer);
            layer.offsetMin = offsetMin;
            layer.offsetMax = offsetMax;

            CreateButtonHalf(name + "Left", layer, sprite, false, color);
            CreateButtonHalf(name + "Right", layer, sprite, true, color);
        }

        private static void CreateButtonHalf(string name, Transform parent, Sprite sprite, bool mirror, Color color)
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
    }
}
