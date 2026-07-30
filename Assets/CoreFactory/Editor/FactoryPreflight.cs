#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using CoreFactory.UI;

namespace CoreFactory.Editor
{
    [InitializeOnLoad]
    public class FactoryPreflight : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;

        static FactoryPreflight()
        {
            // PRE-06 completely resolved: Queue RunChecks inside delayCall right after EnsureGeneratedAssets (avoids false P0 alarms on first boot!)
            EditorApplication.delayCall += () =>
            {
                EnsureGeneratedAssets();
                RunChecks(EditorUserBuildSettings.activeBuildTarget);
            };
        }

        [MenuItem("CoreFactory/Run Project Preflight Checks")]
        public static void RunFromMenu()
        {
            EnsureGeneratedAssets();
            RunChecks(EditorUserBuildSettings.activeBuildTarget);
        }

        public void OnPreprocessBuild(BuildReport report)
        {
            // PRE-01 completely resolved: Actually halt Unity build process by throwing BuildFailedException on non-compliant configurations!
            // PRE-03 completely resolved: Pass target platform dynamically
            // PRE-04 completely resolved: Do NOT generate assets inside pre-build, only verify and throw if missing
            if (!RunChecks(report.summary.platform))
            {
                throw new BuildFailedException("[Preflight] Build stopped due to non-compliant framework settings. Please resolve the P0 Errors listed above in the console log.");
            }
        }

        public static bool RunChecks(BuildTarget target)
        {
            bool pass = true;

            // 1. Android VIBRATE Permission check - strictly runs under Android build targets (PRE-03 fix!)
            if (target == BuildTarget.Android)
            {
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
                    // HAP-01 completely resolved: missing manifest file is now a hard, blocking build failure!
                    Debug.LogError("[Preflight] P0 ERROR: AndroidManifest.xml not found! Android VIBRATE permission cannot be verified.");
                    pass = false;
                }
            }

            // 2. iOS Native ARC flag verification - strictly runs under iOS build targets (PRE-03 fix!)
            if (target == BuildTarget.iOS)
            {
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
                    Debug.LogError("[Preflight] P0 ERROR: HapticBridge.mm not found or failed to load via AssetImporter. Verify iOS haptic bridge asset.");
                    pass = false;
                }
            }

            // 3. 9-Slice Rounded Sprite presence check (No auto-compilation during build, PRE-04 fix!)
            string generatedSprite = Path.Combine(Application.dataPath, "CoreFactory/Resources/Generated/RoundedSquare.png");
            if (!File.Exists(generatedSprite))
            {
                Debug.LogError("[Preflight] P0 ERROR: RoundedSquare.png missing inside Resources/Generated/. Build stopped to prevent visual rendering failure.");
                pass = false;
            }

            // 4. UITheme ScriptableObject presence check (No auto-compilation during build, PRE-04 fix!)
            string themeAsset = Path.Combine(Application.dataPath, "CoreFactory/Resources/UITheme.asset");
            if (!File.Exists(themeAsset))
            {
                Debug.LogError("[Preflight] P0 ERROR: UITheme.asset missing inside Resources/. Build stopped.");
                pass = false;
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

        private static void EnsureGeneratedAssets()
        {
            bool produced = false;

            string generatedSprite = Path.Combine(Application.dataPath, "CoreFactory/Resources/Generated/RoundedSquare.png");
            if (!File.Exists(generatedSprite))
            {
                Debug.Log("[Preflight] Automatically compiling rounded 9-slice sprite assets during editor idle...");
                RoundedSpriteGenerator.GenerateRoundedSprite();
                produced = true;
            }

            string themeAsset = Path.Combine(Application.dataPath, "CoreFactory/Resources/UITheme.asset");
            if (!File.Exists(themeAsset))
            {
                Debug.Log("[Preflight] Automatically compiling default UI theme asset during editor idle...");
                UIThemeGenerator.GenerateThemeAsset();
                produced = true;
            }

            if (produced)
            {
                AssetDatabase.Refresh();
            }
        }
    }
}
#endif