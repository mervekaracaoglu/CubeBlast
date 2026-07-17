using UnityEditor;
using UnityEngine;

namespace CubeBlast.EditorTools
{
    /// <summary>
    /// Guarantees that every texture under Resources/Sprites is imported as a
    /// Sprite with sensible settings, no matter how the project is opened.
    /// Also assigns 9-slice borders to the few UI images that get stretched.
    /// </summary>
    public sealed class SpriteImportPostprocessor : AssetPostprocessor
    {
        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith("Assets/Resources/Sprites"))
            {
                return;
            }
            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.filterMode = FilterMode.Bilinear;
            importer.spritePixelsPerUnit = 100f;
            importer.spriteBorder = BorderFor(assetPath);

            // 9-sliced sprites need a Full Rect mesh to render correctly
            // with SpriteRenderer sliced/tiled draw modes.
            if (importer.spriteBorder.sqrMagnitude > 0f)
            {
                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                settings.spriteMeshType = SpriteMeshType.FullRect;
                importer.SetTextureSettings(settings);
            }
        }

        private static Vector4 BorderFor(string path)
        {
            // x = left, y = bottom, z = right, w = top (pixels).
            if (path.EndsWith("grid_background.png"))
            {
                return new Vector4(30f, 30f, 30f, 30f);
            }
            if (path.EndsWith("UI/Menu/button.png") || path.EndsWith("button_frame.png"))
            {
                return new Vector4(28f, 28f, 28f, 28f);
            }
            if (path.EndsWith("top_ui.png"))
            {
                return new Vector4(90f, 90f, 90f, 90f);
            }
            return Vector4.zero;
        }
    }
}