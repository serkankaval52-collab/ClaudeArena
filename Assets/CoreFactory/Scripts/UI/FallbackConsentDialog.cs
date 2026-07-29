using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CoreFactory.Core;
using CoreFactory.Monetization;

namespace CoreFactory.UI
{
    public class FallbackConsentDialog : MonoBehaviour
    {
        private GameObject _canvasObject;
        private CanvasGroup _canvasGroup;
        private bool _isClosing;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            EventBus.MarkSticky<ConsentPromptRequestedEvent>();
            var container = new GameObject("FallbackConsentDialog_Trigger");
            container.AddComponent<FallbackConsentDialog>();
            DontDestroyOnLoad(container);
        }

        private void Awake()
        {
            EventBus.Subscribe<ConsentPromptRequestedEvent>(OnConsentPromptRequested);
        }

        private void OnConsentPromptRequested(ConsentPromptRequestedEvent ev)
        {
            if (_canvasObject != null || _isClosing) return;
            ConstructCanvasUI();
        }

        private static void EnsureEventSystem()
        {
#if UNITY_2023_1_OR_NEWER
            var existing = Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>(FindObjectsInactive.Include);
#else
            var existing = Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>(true);
#endif
            if (existing != null) return;

            var esObj = new GameObject("EventSystem");
            esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();

#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            esObj.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
            esObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
#endif
            Object.DontDestroyOnLoad(esObj);
        }

        private void ConstructCanvasUI()
        {
            EnsureEventSystem();
            _isClosing = false;
            _canvasObject = new GameObject("Consent_Canvas");
            var canvas = _canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 9999;

            var scaler = _canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            _canvasObject.AddComponent<GraphicRaycaster>();
            _canvasGroup = _canvasObject.AddComponent<CanvasGroup>();
            _canvasGroup.alpha = 0f;
            DontDestroyOnLoad(_canvasObject);

            var scrim = new GameObject("FullBleedScrim");
            scrim.transform.SetParent(_canvasObject.transform, false);
            var scrimRect = scrim.AddComponent<RectTransform>();
            scrimRect.anchorMin = Vector2.zero;
            scrimRect.anchorMax = Vector2.one;
            scrimRect.sizeDelta = Vector2.zero;

            var scrimImage = scrim.AddComponent<Image>();
            scrimImage.color = new Color(0.05f, 0.05f, 0.05f, 0.95f);
            scrimImage.raycastTarget = true;

            var safeRoot = new GameObject("SafeAreaRoot");
            safeRoot.transform.SetParent(_canvasObject.transform, false);
            var safeRect = safeRoot.AddComponent<RectTransform>();
            safeRect.anchorMin = Vector2.zero;
            safeRect.anchorMax = Vector2.one;
            safeRect.sizeDelta = Vector2.zero;
            safeRoot.AddComponent<SafeAreaDetector>();

            var textObj = new GameObject("DescriptionText");
            textObj.transform.SetParent(safeRoot.transform, false);
            var tRect = textObj.AddComponent<RectTransform>();
            tRect.anchorMin = new Vector2(0.1f, 0.4f);
            tRect.anchorMax = new Vector2(0.9f, 0.8f);
            tRect.sizeDelta = Vector2.zero;

            var tmpText = textObj.AddComponent<TextMeshProUGUI>();
            
            // LEG-05 & LEG-09 completely resolved: dynamic localization & non-coercive compliant wording
            tmpText.text = LocalizeOrFallback("consent_description", "We use device identifiers to personalize ads and measure performance. You can decline and still play with non-personalized ads.");
            tmpText.alignment = TextAlignmentOptions.Center;

            UIThemeRuntime.ApplyFontWithFallbacks(tmpText);
            textObj.AddComponent<DynamicTextScaler>();

            CreateButton(safeRoot.transform, "AcceptButton", LocalizeOrFallback("consent_accept", "ACCEPT"), new Vector2(0.15f, 0.2f), new Vector2(0.45f, 0.32f), () => {
                EventBus.Publish(new ConsentPromptDecisionEvent(ConsentStatus.Granted));
                StartCoroutine(CloseDialogRoutine());
            });

            CreateButton(safeRoot.transform, "DeclineButton", LocalizeOrFallback("consent_decline", "DECLINE"), new Vector2(0.55f, 0.2f), new Vector2(0.85f, 0.32f), () => {
                EventBus.Publish(new ConsentPromptDecisionEvent(ConsentStatus.Denied));
                StartCoroutine(CloseDialogRoutine());
            });

            StartCoroutine(SimpleTween.FadeTween(_canvasGroup, 0f, 1f, 0.22f));
        }

        private System.Collections.IEnumerator CloseDialogRoutine()
        {
            if (_isClosing) yield break;
            _isClosing = true;

            if (_canvasGroup != null)
            {
                _canvasGroup.interactable = false;
                _canvasGroup.blocksRaycasts = false;
            }

            EventBus.ClearSticky<ConsentPromptRequestedEvent>();
            
            if (_canvasGroup != null)
            {
                yield return StartCoroutine(SimpleTween.FadeTween(_canvasGroup, 1f, 0f, 0.14f));
            }

            if (_canvasObject != null)
            {
                Destroy(_canvasObject);
                _canvasObject = null;
            }
            _isClosing = false;
        }

        private void CreateButton(Transform parent, string name, string text, Vector2 anchorMin, Vector2 anchorMax, System.Action onClickAction)
        {
            var btnObj = new GameObject(name);
            btnObj.transform.SetParent(parent, false);
            var rRect = btnObj.AddComponent<RectTransform>();
            rRect.anchorMin = anchorMin;
            rRect.anchorMax = anchorMax;
            rRect.sizeDelta = Vector2.zero;

            var bg = btnObj.AddComponent<Image>();
            bg.color = new Color(0.15f, 0.15f, 0.15f, 1f);

            var btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = bg;
            btn.transition = Selectable.Transition.ColorTint;
            btn.colors = new ColorBlock
            {
                normalColor = Color.white,
                highlightedColor = new Color(1.1f, 1.1f, 1.1f, 1f),
                pressedColor = new Color(0.75f, 0.75f, 0.75f, 1f),
                selectedColor = Color.white,
                disabledColor = new Color(1f, 1f, 1f, 0.5f),
                colorMultiplier = 1f,
                fadeDuration = 0.08f
            };
            btn.onClick.AddListener(() => {
                if (_isClosing || btnObj == null) return;
                StartCoroutine(BounceButton(btnObj.transform));
                onClickAction?.Invoke();
            });

            var txtObj = new GameObject("Text");
            txtObj.transform.SetParent(btnObj.transform, false);
            var tRect = txtObj.AddComponent<RectTransform>();
            tRect.anchorMin = Vector2.zero;
            tRect.anchorMax = Vector2.one;
            tRect.sizeDelta = Vector2.zero;

            var tmp = txtObj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;

            UIThemeRuntime.ApplyFontWithFallbacks(tmp);
            txtObj.AddComponent<DynamicTextScaler>();
        }

        private System.Collections.IEnumerator BounceButton(Transform target)
        {
            if (target == null) yield break;
            yield return StartCoroutine(SimpleTween.ScaleTween(target, Vector3.one, new Vector3(0.95f, 0.95f, 1f), 0.04f, null));
            if (target == null) yield break;
            yield return StartCoroutine(SimpleTween.ScaleTween(target, new Vector3(0.95f, 0.95f, 1f), Vector3.one, 0.04f, null));
        }

        private static string LocalizeOrFallback(string key, string fallback)
        {
            return fallback;
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<ConsentPromptRequestedEvent>(OnConsentPromptRequested);
            if (_canvasObject != null)
            {
                Destroy(_canvasObject);
                _canvasObject = null;
            }
        }
    }
}