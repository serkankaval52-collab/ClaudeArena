#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;
using CoreFactory.UI;

namespace CoreFactory.Editor
{
    [InitializeOnLoad]
    public static class FactoryPreflight
    {
        static FactoryPreflight()
        {
            RunChecks();
        }

        [MenuItem("CoreFactory/Run Project Preflight Checks")]
        public static bool RunChecks()
        {
            bool pass = true;

            // 1. Android JNI vibration permission (HAP-01 check)
            string manifestPath = Path.Combine(Application.dataPath, "Plugins/Android/AndroidManifest.xml");
            if (File.Exists(manifestPath))
            {
                string content = File.ReadAllText(manifestPath);
                if (!content.Contains("android.permission.VIBRATE"))
                {
                    Debug.LogError("[Preflight] P0 ERROR: AndroidManifest.xml is missing <uses-permission android:name=\"android.permission.VIBRATE\" />! Android JNI haptics will fail at runtime.");
                    pass = false;
                }
            }
            else
            {
                Debug.LogWarning("[Preflight] WARNING: AndroidManifest.xml not found. Confirm that the permission VIBRATE is registered in your final Android package.");
            }

            // 2. iOS Native ARC flag verification (IOS-01 check using real Unity APIs!) (PRE-02 fix!)
            string pluginPath = "Assets/Plugins/iOS/HapticBridge.mm";
            PluginImporter pi = AssetImporter.GetAtPath(pluginPath) as PluginImporter;
            if (pi != null)
            {
                string flags = pi.GetPlatformData(BuildTarget.iOS, "CompileFlags");
                if (string.IsNullOrEmpty(flags) || !flags.Contains("-fobjc-arc"))
                {
                    Debug.LogError("[Preflight] P0 ERROR: HapticBridge.mm is missing compileFlags '-fobjc-arc'! Objective-C will leak.");
                    pass = false;
                }
            }
            else
            {
                Debug.LogWarning("[Preflight] WARNING: HapticBridge.mm not found or failed to load via AssetImporter.");
            }

            // 3. Automated 9-Slice Rounded Sprite generation
            string generatedSprite = Path.Combine(Application.dataPath, "CoreFactory/Resources/Generated/RoundedSquare.png");
            if (!File.Exists(generatedSprite))
            {
                Debug.Log("[Preflight] Automatically compiling rounded 9-slice sprite assets...");
                RoundedSpriteGenerator.GenerateRoundedSprite();
            }

            // 4. Automated UITheme ScriptableObject asset compilation (VIS-03 check)
            string themeAsset = Path.Combine(Application.dataPath, "CoreFactory/Resources/UITheme.asset");
            if (!File.Exists(themeAsset))
            {
                Debug.Log("[Preflight] UITheme.asset missing inside Resources. Automatically compiling default UI theme asset...");
                UIThemeGenerator.GenerateThemeAsset();
            }
            else
            {
                // Verify that primaryFont and fallbacks are assigned
                UIThemeAsset theme = AssetDatabase.LoadAssetAtPath<UIThemeAsset>("Assets/CoreFactory/Resources/UITheme.asset");
                if (theme != null && (theme.primaryFont == null || theme.fallbackFonts == null || theme.fallbackFonts.Length == 0))
                {
                    Debug.LogWarning("[Preflight] WARNING: UITheme.asset has null font references. ja/ko/zh/ar will render as tofu (VIS-03).");
                }
            }

            if (pass)
            {
                Debug.Log("[Preflight] PASS: All compile-time validation checks passed cleanly.");
            }
            return pass;
        }
    }
}
#endif