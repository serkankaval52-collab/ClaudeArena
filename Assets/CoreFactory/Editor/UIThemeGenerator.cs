#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace CoreFactory.UI
{
    public static class UIThemeGenerator
    {
        [MenuItem("CoreFactory/Generate Default UI Theme")]
        public static void GenerateThemeAsset()
        {
            string dir = Path.Combine(Application.dataPath, "CoreFactory/Resources");
            Directory.CreateDirectory(dir);

            string assetPath = "Assets/CoreFactory/Resources/UITheme.asset";
            UIThemeAsset theme = AssetDatabase.LoadAssetAtPath<UIThemeAsset>(assetPath);

            if (theme == null)
            {
                theme = ScriptableObject.CreateInstance<UIThemeAsset>();
                
                // Programmatically configure default type ramps and color palettes to prevent tofu (VIS-03)
                theme.background = new Color32(0x0B, 0x0C, 0x10, 0xFF);
                theme.surface = new Color32(0x1F, 0x28, 0x33, 0xFF);
                theme.textPrimary = new Color32(0xC5, 0xC6, 0xC7, 0xFF);
                theme.accent = new Color32(0x66, 0xFC, 0xF1, 0xFF);
                theme.accentDeep = new Color32(0x45, 0xA2, 0x9E, 0xFF);
                theme.scrim = new Color(0f, 0f, 0f, 0.86f);

                theme.displaySize = 96f;
                theme.titleSize = 64f;
                theme.headingSize = 48f;
                theme.bodySize = 36f;
                theme.captionSize = 28f;

                theme.spacingXs = 8f;
                theme.spacingSm = 16f;
                theme.spacingMd = 24f;
                theme.spacingLg = 40f;
                theme.spacingXl = 64f;

                theme.cornerRadius = 24f;
                theme.modalInDuration = 0.22f;
                theme.modalOutDuration = 0.14f;
                theme.buttonPressDuration = 0.08f;
                theme.buttonPressScale = 0.96f;

                // VIS-03 search and assign fallback if TMPro essential assets exist
                var defaultFont = AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
                if (defaultFont != null)
                {
                    theme.primaryFont = defaultFont;
                }

                AssetDatabase.CreateAsset(theme, assetPath);
                Debug.Log($"[UIThemeGenerator] Created new UITheme.asset at {assetPath}");
            }
            else
            {
                Debug.Log($"[UIThemeGenerator] UITheme.asset already exists at {assetPath}. Skip creation.");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }
}
#endif