using UnityEngine;
using TMPro;

namespace CoreFactory.UI
{
    [CreateAssetMenu(fileName = "UITheme", menuName = "CoreFactory/UI Theme")]
    public class UIThemeAsset : ScriptableObject
    {
        [Header("Palette")]
        public Color background = new Color32(0x0B, 0x0C, 0x10, 0xFF);
        public Color surface = new Color32(0x1F, 0x28, 0x33, 0xFF);
        public Color textPrimary = new Color32(0xC5, 0xC6, 0xC7, 0xFF);
        public Color accent = new Color32(0x66, 0xFC, 0xF1, 0xFF);
        public Color accentDeep = new Color32(0x45, 0xA2, 0x9E, 0xFF);
        public Color scrim = new Color(0f, 0f, 0f, 0.86f);

        [Header("Type Ramp")]
        public float displaySize = 96f;
        public float titleSize = 64f;
        public float headingSize = 48f;
        public float bodySize = 36f;
        public float captionSize = 28f;

        [Header("Spacing scale")]
        public float spacingXs = 8f;
        public float spacingSm = 16f;
        public float spacingMd = 24f;
        public float spacingLg = 40f;
        public float spacingXl = 64f;

        [Header("Radius & Motion")]
        public float cornerRadius = 24f;
        public float modalInDuration = 0.22f;
        public float modalOutDuration = 0.14f;
        public float buttonPressDuration = 0.08f;
        public float buttonPressScale = 0.96f;

        [Header("Fonts")]
        public TMP_FontAsset primaryFont;
        public TMP_FontAsset[] fallbackFonts;
        public float minTouchTargetMm = 9f;
    }
}