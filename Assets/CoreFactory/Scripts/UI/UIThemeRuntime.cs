using UnityEngine;
using TMPro;

namespace CoreFactory.UI
{
    public static class UIThemeRuntime
    {
        private static UIThemeAsset _theme;
        private static bool _fallbacksWired;

        public static UIThemeAsset Theme
        {
            get
            {
                if (_theme == null)
                {
                    _theme = Resources.Load<UIThemeAsset>("UITheme");
                }
                return _theme;
            }
        }

        public static void ApplyFontWithFallbacks(TMP_Text text)
        {
            if (text == null) return;
            if (TMP_Settings.instance == null)
            {
                Debug.LogError("[UIThemeRuntime] TMP Settings missing. Please Import TMP Essential Resources.");
                return;
            }

            var theme = Theme;
            if (theme == null || theme.primaryFont == null) return;

            text.font = theme.primaryFont;
            WireGlobalFallbacks(theme);
        }

        private static void WireGlobalFallbacks(UIThemeAsset theme)
        {
            if (_fallbacksWired) return;
            if (theme.fallbackFonts == null || theme.fallbackFonts.Length == 0) return;

            _fallbacksWired = true;
            var list = theme.primaryFont.fallbackFontAssetTable;
            if (list == null)
            {
                list = new System.Collections.Generic.List<TMP_FontAsset>();
                theme.primaryFont.fallbackFontAssetTable = list;
            }
            foreach (var fb in theme.fallbackFonts)
            {
                if (fb != null && !list.Contains(fb)) list.Add(fb);
            }
        }

        public static void ApplyTextDirection(TMP_Text text, string localeCode)
        {
            if (text == null) return;
            bool isRtl = localeCode is "ar" or "he" or "fa" or "ur";
            text.isRightToLeftText = isRtl;
            if (isRtl) text.alignment = TextAlignmentOptions.Right;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            _theme = null;
            _fallbacksWired = false;
        }
    }
}