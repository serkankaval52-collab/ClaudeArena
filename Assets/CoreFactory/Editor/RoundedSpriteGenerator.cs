#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace CoreFactory.Editor
{
    public static class RoundedSpriteGenerator
    {
        [MenuItem("CoreFactory/Generate 9-Slice Rounded Sprite")]
        public static void GenerateRoundedSprite()
        {
            int size = 128;
            int radius = 24; // Align radius exactly with theme token (cornerRadius = 24f) (VIS-13b fix!)
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color[] colors = texture.GetPixels();

            for (int i = 0; i < colors.Length; i++)
            {
                colors[i] = Color.clear;
            }
            texture.SetPixels(colors);

            Color fillColor = Color.white;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    if (IsInsideRoundedCorner(x, y, size, radius))
                    {
                        float dist = GetDistanceToEdge(x, y, size, radius);
                        if (dist < 0f)
                        {
                            texture.SetPixel(x, y, fillColor);
                        }
                        else if (dist < 1.0f)
                        {
                            float alpha = Mathf.Clamp01(1.0f - dist);
                            texture.SetPixel(x, y, new Color(fillColor.r, fillColor.g, fillColor.b, alpha));
                        }
                    }
                }
            }

            texture.Apply();

            // Write into Resources/Generated to let Resources.Load<Sprite>() fetch it correctly (VIS-13b fix!)
            string dir = Path.Combine(Application.dataPath, "CoreFactory/Resources/Generated");
            Directory.CreateDirectory(dir);
            string path = Path.Combine(dir, "RoundedSquare.png");
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);

            AssetDatabase.Refresh();

            string relativePath = "Assets/CoreFactory/Resources/Generated/RoundedSquare.png";
            TextureImporter importer = AssetImporter.GetAtPath(relativePath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spriteBorder = new Vector4(radius, radius, radius, radius);
                importer.filterMode = FilterMode.Bilinear;
                importer.mipmapEnabled = false;
                importer.SaveAndReimport();
            }

            Debug.Log($"[RoundedSpriteGenerator] Compiled RoundedSquare.png inside Resources/Generated/ at {relativePath}");
        }

        private static bool IsInsideRoundedCorner(int x, int y, int size, int radius)
        {
            if (x < radius && y < radius) return IsInsideCircle(x, y, radius, radius, radius);
            if (x > size - radius - 1 && y < radius) return IsInsideCircle(x, y, size - radius - 1, radius, radius);
            if (x < radius && y > size - radius - 1) return IsInsideCircle(x, y, radius, size - radius - 1, radius);
            if (x > size - radius - 1 && y > size - radius - 1) return IsInsideCircle(x, y, size - radius - 1, size - radius - 1, radius);
            return true;
        }

        private static bool IsInsideCircle(int x, int y, int cx, int cy, int r)
        {
            return (x - cx) * (x - cx) + (y - cy) * (y - cy) <= r * r;
        }

        private static float GetDistanceToEdge(int x, int y, int size, int radius)
        {
            float dx = 0, dy = 0;
            if (x < radius && y < radius) { dx = x - radius; dy = y - radius; }
            else if (x > size - radius - 1 && y < radius) { dx = x - (size - radius - 1); dy = y - radius; }
            else if (x < radius && y > size - radius - 1) { dx = x - radius; dy = y - (size - radius - 1); }
            else if (x > size - radius - 1 && y > size - radius - 1) { dx = x - (size - radius - 1); dy = y - (size - radius - 1); }
            else return -1.0f;

            float dist = Mathf.Sqrt(dx * dx + dy * dy);
            return dist - radius;
        }
    }
}
#endif