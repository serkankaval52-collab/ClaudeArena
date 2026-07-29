using UnityEngine;

namespace CoreFactory.UI
{
    [RequireComponent(typeof(RectTransform))]
    public class SafeAreaDetector : MonoBehaviour
    {
        private RectTransform _rectTransform;
        private Rect _lastSafeArea = Rect.zero;
        private ScreenOrientation _lastOrientation;
        private Vector2Int _lastResolution;
        private float _nextCheckTime;
        private const float CheckInterval = 0.25f;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            ApplySafeArea();
        }

        private void Update()
        {
            if (Time.unscaledTime < _nextCheckTime) return;
            _nextCheckTime = Time.unscaledTime + CheckInterval;

            if (_lastSafeArea != Screen.safeArea ||
                _lastOrientation != Screen.orientation ||
                _lastResolution != new Vector2Int(Screen.width, Screen.height))
            {
                ApplySafeArea();
            }
        }

        private void ApplySafeArea()
        {
            int w = Screen.width;
            int h = Screen.height;
            if (w <= 0 || h <= 0) return;

            Rect safeArea = Screen.safeArea;
            if (safeArea.width <= 0f || safeArea.height <= 0f) return;

            _lastSafeArea = safeArea;
            _lastOrientation = Screen.orientation;
            _lastResolution = new Vector2Int(w, h);

            Vector2 anchorMin = safeArea.position;
            Vector2 anchorMax = safeArea.position + safeArea.size;

            anchorMin.x /= w;
            anchorMin.y /= h;
            anchorMax.x /= w;
            anchorMax.y /= h;

            if (float.IsNaN(anchorMin.x) || float.IsNaN(anchorMin.y) ||
                float.IsNaN(anchorMax.x) || float.IsNaN(anchorMax.y))
            {
                return;
            }

            _rectTransform.anchorMin = anchorMin;
            _rectTransform.anchorMax = anchorMax;
        }
    }
}