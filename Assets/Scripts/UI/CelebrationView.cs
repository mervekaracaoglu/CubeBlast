using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CubeBlast.Game
{
    /// <summary>
    /// Win celebration: a bursting star, "Perfect!" text and several firework
    /// waves, then waits for the player to tap before returning to MainScene.
    /// </summary>
    public static class CelebrationView
    {
        private const float AutoAdvanceSeconds = 6f;

        public static IEnumerator Play(Transform canvas, MonoBehaviour host)
        {
            var dim = UiFactory.CreateImage("WinDim", canvas, null);
            dim.sprite = null;
            dim.color = Color.clear;
            UiFactory.Stretch(dim.rectTransform);

            // Create a matching UI dimming overlay for the top HUD panel
            var hudDim = UiFactory.CreateImage("HudDim", dim.transform, null);
            hudDim.sprite = null;
            hudDim.color = new Color(0f, 0f, 0f, 0f);
            var hudDimRect = hudDim.rectTransform;
            hudDimRect.anchorMin = new Vector2(0.5f, 1f);
            hudDimRect.anchorMax = new Vector2(0.5f, 1f);
            hudDimRect.pivot = new Vector2(0.5f, 1f);
            hudDimRect.anchoredPosition = Vector2.zero;
            hudDimRect.sizeDelta = new Vector2(1080f, 430f);

            // Create a world-space visual dimming background (drawn behind particles at order 215)
            var dimGo = new GameObject("WinDimWorld");
            var dimRenderer = dimGo.AddComponent<SpriteRenderer>();
            var whiteTex = new Texture2D(1, 1);
            whiteTex.SetPixel(0, 0, Color.white);
            whiteTex.Apply();
            dimRenderer.sprite = Sprite.Create(whiteTex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
            dimRenderer.sortingOrder = 215;
            dimRenderer.color = new Color(0f, 0f, 0f, 0f);

            var camera = Camera.main;
            if (camera != null)
            {
                float height = camera.orthographicSize * 2f;
                float width = height * ((float)Screen.width / Screen.height);
                dimGo.transform.localScale = new Vector3(width + 2f, height + 2f, 1f);
                dimGo.transform.position = new Vector3(camera.transform.position.x, camera.transform.position.y, 0f);
            }

            Tween.To(t =>
            {
                float alpha = 0.80f * t;
                if (dimRenderer != null) { dimRenderer.color = new Color(0f, 0f, 0f, alpha); }
                if (hudDim != null) { hudDim.color = new Color(0f, 0f, 0f, alpha); }
            }, 0.3f);

            // Create a child container under dim for all interactive UI elements (star, labels, buttons)
            // so we can destroy them instantly on tap while leaving the dark background overlays active during scene load.
            var winBanners = UiFactory.CreateRect("WinBanners", dim.transform);
            UiFactory.Stretch(winBanners);

            // Big star pops in and slowly spins.
            var star = UiFactory.CreateImage("Star", winBanners, GameAssets.CelebrationStar);
            var starRect = star.rectTransform;
            starRect.anchorMin = starRect.anchorMax = new Vector2(0.5f, 0.55f);
            starRect.sizeDelta = new Vector2(430f, 430f);
            star.preserveAspect = true;
            star.raycastTarget = false;
            star.transform.localScale = Vector3.zero;
            Tween.Scale(star.transform, Vector3.one, 0.45f, Ease.OutBack, 0.05f);
            Tween.Rotate(star.transform, 360f, 3f, Ease.InOutQuad);

            var label = UiFactory.CreateText("WinLabel", winBanners, "Perfect!", 96, new Color(1f, 0.9f, 0.25f));
            var labelRect = label.rectTransform;
            labelRect.anchorMin = labelRect.anchorMax = new Vector2(0.5f, 0.33f);
            labelRect.anchoredPosition = Vector2.zero;
            label.transform.localScale = Vector3.zero;
            Tween.Scale(label.transform, Vector3.one, 0.4f, Ease.OutBack, 0.25f);

            // Firework waves in world space around the board (clean star sprites only).
            camera = Camera.main;
            var sprites = new[] { GameAssets.RocketStar, GameAssets.CelebrationStar };
            for (int wave = 0; wave < 5; wave++)
            {
                Vector3 position = Vector3.zero;
                if (camera != null)
                {
                    var viewport = new Vector3(Random.Range(0.2f, 0.8f), Random.Range(0.3f, 0.8f), 10f);
                    position = camera.ViewportToWorldPoint(viewport);
                    position.z = 0f;
                }
                Fx.Burst(position, sprites, 22, 0.75f, 6.5f, 5f, 0.9f, new Color(1f, 0.92f, 0.15f));
                yield return new WaitForSeconds(0.32f);
            }
            yield return new WaitForSeconds(0.25f);

            // "Tap to Continue" hint fades in; the entire dim layer becomes a button.
            bool tapped = false;
            var tapLabel = UiFactory.CreateText("TapLabel", winBanners, "Tap to Continue", 46,
                new Color(1f, 1f, 1f, 0f));
            var tapRect = tapLabel.rectTransform;
            tapRect.anchorMin = tapRect.anchorMax = new Vector2(0.5f, 0.17f);
            tapRect.anchoredPosition = Vector2.zero;
            Tween.To(t =>
            {
                if (tapLabel != null) { tapLabel.color = new Color(1f, 1f, 1f, t * 0.85f); }
            }, 0.5f);

            // Transparent full-screen tap area on top of everything.
            var tapBtn = UiFactory.CreateButton("TapArea", winBanners, null, () => { tapped = true; });
            UiFactory.Stretch((RectTransform)tapBtn.transform);
            tapBtn.image.color = Color.clear;

            // Continue one more firework burst while waiting for input.
            float elapsed = 0f;
            float nextBurst = 0.6f;
            while (!tapped && elapsed < AutoAdvanceSeconds)
            {
                elapsed += Time.deltaTime;
                nextBurst -= Time.deltaTime;
                if (nextBurst <= 0f)
                {
                    nextBurst = 1.2f;
                    if (camera != null)
                    {
                        var vp = new Vector3(Random.Range(0.15f, 0.85f), Random.Range(0.3f, 0.85f), 10f);
                        var pos = camera.ViewportToWorldPoint(vp);
                        pos.z = 0f;
                        Fx.Burst(pos, sprites, 15, 0.65f, 5.5f, 5f, 0.8f, new Color(1f, 0.92f, 0.15f));
                    }
                }
                yield return null;
            }

            // Cleanup UI banners immediately to prevent clashing during transition.
            if (winBanners != null)
            {
                Object.Destroy(winBanners.gameObject);
            }

            // Load the next scene. We leave the dark background overlays active so they continue
            // covering the board until the new scene loads and unloads the current scene automatically.
            SceneManager.LoadScene("MainScene");
        }
    }
}
