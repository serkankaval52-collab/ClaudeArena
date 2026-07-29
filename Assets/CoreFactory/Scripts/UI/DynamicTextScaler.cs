using UnityEngine;
using TMPro;

namespace CoreFactory.UI
{
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class DynamicTextScaler : MonoBehaviour
    {
        public enum TextRole { Display, Title, Heading, Body, Caption }
        [SerializeField] private TextRole role = TextRole.Body;
        [SerializeField] private bool allowAutoSize = true;
        [Range(0.5f, 1f)] [SerializeField] private float minSizeRatio = 0.75f;

        private TextMeshProUGUI _text;

        private void Awake()
        {
            _text = GetComponent<TextMeshProUGUI>();
            Configure();
        }

        public void Configure()
        {
            var theme = UIThemeRuntime.Theme;
            float baseSize = theme != null ? GetRampSize(theme) : 36f;
            _text.fontSize = baseSize;

            if (allowAutoSize)
            {
                _text.enableAutoSizing = true;
                _text.fontSizeMin = baseSize * minSizeRatio;
                _text.fontSizeMax = baseSize;
            }
            else
            {
                _text.enableAutoSizing = false;
            }

            _text.enableWordWrapping = true;
            _text.overflowMode = TextOverflowModes.Ellipsis;
        }

        private float GetRampSize(UIThemeAsset t) => role switch
        {
            TextRole.Display => t.displaySize,
            TextRole.Title => t.titleSize,
            TextRole.Heading => t.headingSize,
            TextRole.Body => t.bodySize,
            TextRole.Caption => t.captionSize,
            _ => t.bodySize
        };
    }
}