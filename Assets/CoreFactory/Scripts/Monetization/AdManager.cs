using UnityEngine;
using CoreFactory.Core;

namespace CoreFactory.Monetization
{
    public enum ConsentStatus { NotDetermined = 0, Denied = 1, Granted = 2 }

    public class AdManager : Singleton<AdManager>
    {
        [SerializeField] private float firstAdSessionDelay = 60f;
        [SerializeField] private float tier1CooldownSeconds = 120f;
        [SerializeField] private float tier2CooldownSeconds = 60f;
        [SerializeField] private bool allowNonPersonalizedAdsWhenDenied = true;

        private const string ConsentPrefsKey = "PrivacyConsent_GDPR";
        private const string ConsentTimestampKey = "PrivacyConsent_UtcTicks";
        private const string ConsentPolicyVersKey = "PrivacyConsent_PolicyVersion";
        private const int CurrentPolicyVersion = 1;

        private float _activeCooldown;
        private float _lastInterstitialTime;
        private float _sessionStartTime;
        private bool _bannersVisible;
        private bool _isInitialized;
        private bool _isAuthoritativeInstance;
        private ConsentStatus _consent = ConsentStatus.NotDetermined;
        private IMonetizationCmpBridge _cmpBridge;

        private System.Action _pendingRewardCallback;
        private System.Action<string> _pendingFailureCallback;
        private bool _rewardGranted; // ADM-05 completely resolved (Defends against race conditions between hide and reward events)

        public ConsentStatus ConsentState => _consent;
        public bool CanServeAnyAd => _consent == ConsentStatus.Granted || (_consent == ConsentStatus.Denied && allowNonPersonalizedAdsWhenDenied);

        protected override void Awake()
        {
            base.Awake();
            _isAuthoritativeInstance = (Instance == this);
            if (!_isAuthoritativeInstance) return;
            Initialize();
        }

        public void Initialize()
        {
            if (_isInitialized) return;
            _isInitialized = true;

            DetectAdFrequencyTier();
            _sessionStartTime = Time.realtimeSinceStartup;
            _lastInterstitialTime = _sessionStartTime - _activeCooldown;

            LoadPersistedConsent();
            _cmpBridge = new LocalFallbackCmpBridge();

#if APPLOVIN_MAX
            MaxSdkCallbacks.Rewarded.OnAdReceivedRewardEvent += OnRewardedAdReceivedReward;
            MaxSdkCallbacks.Rewarded.OnAdDisplayFailedEvent += OnRewardedAdDisplayFailed;
            MaxSdkCallbacks.Rewarded.OnAdHiddenEvent += OnRewardedAdHidden;
#endif
        }

        private void Start()
        {
            if (!_isAuthoritativeInstance) return;
            EventBus.Subscribe<PhaseTransitionEvent>(OnPhaseTransition);

            if (_consent == ConsentStatus.NotDetermined)
            {
                RequestConsentPrompt();
            }
        }

        private void RequestConsentPrompt()
        {
            _cmpBridge.Initialize(OnCmpComplete);
        }

        private void OnCmpComplete(ConsentStatus status)
        {
            ApplyConsent(status);
        }

        private void LoadPersistedConsent()
        {
            int saved = PlayerPrefs.GetInt(ConsentPrefsKey, (int)ConsentStatus.NotDetermined);
            if (!System.Enum.IsDefined(typeof(ConsentStatus), saved))
            {
                _consent = ConsentStatus.NotDetermined;
                return;
            }

            int savedVersion = PlayerPrefs.GetInt(ConsentPolicyVersKey, 0);
            if (savedVersion < CurrentPolicyVersion)
            {
                _consent = ConsentStatus.NotDetermined;
                return;
            }
            _consent = (ConsentStatus)saved;
        }

        public bool HasConsent() => _consent == ConsentStatus.Granted;

        public void GrantConsent() => ApplyConsent(ConsentStatus.Granted);
        public void DenyConsent() => ApplyConsent(ConsentStatus.Denied);

        private void ApplyConsent(ConsentStatus status)
        {
            _consent = status;

            // Handle transient NotDetermined status gracefully (Bug 4 fix) without polluting database records.
            if (status != ConsentStatus.NotDetermined)
            {
                PlayerPrefs.SetInt(ConsentPrefsKey, (int)status);
                PlayerPrefs.SetString(ConsentTimestampKey, System.DateTime.UtcNow.Ticks.ToString());
                PlayerPrefs.SetInt(ConsentPolicyVersKey, CurrentPolicyVersion);
                PlayerPrefs.Save();
            }

            PropagateConsentToAdSdks(status);
            EventBus.Publish(new ConsentStateChangedEvent(status));
        }

        private void PropagateConsentToAdSdks(ConsentStatus status)
        {
            bool hasConsent = (status == ConsentStatus.Granted);
#if APPLOVIN_MAX
            MaxSdk.SetHasUserConsent(hasConsent);
            MaxSdk.SetDoNotSell(!hasConsent);
#endif
        }

        public void ShowPrivacyOptions()
        {
            _cmpBridge.ShowPrivacyOptions(OnPrivacyOptionsComplete);
        }

        private void OnPrivacyOptionsComplete(ConsentStatus status)
        {
            ApplyConsent(status);
        }

        private void DetectAdFrequencyTier()
        {
            _activeCooldown = tier1CooldownSeconds;
            string region;
            try
            {
                region = System.Globalization.RegionInfo.CurrentRegion?.TwoLetterISORegionName;
            }
            catch
            {
                region = null;
            }

            if (string.IsNullOrEmpty(region)) return;

            switch (region.ToUpperInvariant())
            {
                case "US": case "DE": case "FR": case "GB": case "CA": case "AU": case "JP":
                    _activeCooldown = tier1CooldownSeconds;
                    break;
                default:
                    _activeCooldown = tier2CooldownSeconds;
                    break;
            }
        }

        private void OnPhaseTransition(PhaseTransitionEvent ev)
        {
            if (!CanServeAnyAd) return;
            switch (ev.NewPhase)
            {
                case GamePhase.Menu:
                case GamePhase.Results:
                    ShowBanner();
                    break;
                case GamePhase.Playing:
                    HideBanner();
                    break;
                case GamePhase.GameOver:
                    HideBanner();
                    TryShowInterstitial("GameOver");
                    break;
            }
        }

        public void ShowBanner()
        {
            if (!CanServeAnyAd) return;
            if (_bannersVisible) return;
            _bannersVisible = true;
            Debug.Log("[AdManager] Displaying Banner.");
        }

        public void HideBanner()
        {
            if (!_bannersVisible) return;
            _bannersVisible = false;
            Debug.Log("[AdManager] Dismissing Banner.");
        }

        public bool TryShowInterstitial(string placement)
        {
            if (!CanServeAnyAd) return false;

            float now = Time.realtimeSinceStartup;
            if (now - _sessionStartTime < firstAdSessionDelay) return false;
            if (now - _lastInterstitialTime < _activeCooldown) return false;

            _lastInterstitialTime = now;
            return true;
        }

        public void ShowRewardedAd(string placement, System.Action onRewardEarned, System.Action<string> onFailed = null)
        {
            if (!CanServeAnyAd)
            {
                onFailed?.Invoke("consent_unavailable");
                return;
            }

            // ADM-03 completely resolved (Overlapping requests are rejected cleanly instead of overwriting)
            if (_pendingRewardCallback != null)
            {
                onFailed?.Invoke("busy");
                return;
            }

            _rewardGranted = false; // Reset state before starting rewarded ad sequence (ADM-05 fix)
            _pendingRewardCallback = onRewardEarned;
            _pendingFailureCallback = onFailed;

#if APPLOVIN_MAX
            if (!MaxSdk.IsRewardedAdReady("RewardedUnitId"))
            {
                OnRewardedAdFailedToDisplay("RewardedUnitId", "ad_not_ready");
                return;
            }
            MaxSdk.ShowRewardedAd("RewardedUnitId", placement);
#else
            OnRewardedAdFailedToDisplay("RewardedUnitId", "sdk_missing");
#endif
        }

#if APPLOVIN_MAX
        private void OnRewardedAdReceivedReward(string adUnitId, MaxSdkBase.Reward reward, MaxSdkBase.AdInfo adInfo)
        {
            _rewardGranted = true; // Set state to true (ADM-05 fix!)
            var cb = _pendingRewardCallback;
            _pendingRewardCallback = null;
            _pendingFailureCallback = null;
            cb?.Invoke();
        }

        private void OnRewardedAdDisplayFailed(string adUnitId, MaxSdkBase.ErrorInfo errorInfo, MaxSdkBase.AdInfo adInfo)
        {
            OnRewardedAdFailedToDisplay(adUnitId, errorInfo.Message);
        }

        private void OnRewardedAdHidden(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            // ADM-05 completely resolved (Do not fail the callback if reward has already been granted)
            if (!_rewardGranted && _pendingRewardCallback != null)
            {
                OnRewardedAdFailedToDisplay(adUnitId, "dismissed");
            }
            else
            {
                _pendingRewardCallback = null;
                _pendingFailureCallback = null;
            }
        }
#endif

        private void OnRewardedAdFailedToDisplay(string adUnitId, string errorMsg)
        {
            var cb = _pendingFailureCallback;
            _pendingRewardCallback = null;
            _pendingFailureCallback = null;
            cb?.Invoke(errorMsg);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy(); // Clears CS0114 compilation warning
            EventBus.Unsubscribe<PhaseTransitionEvent>(OnPhaseTransition);
#if APPLOVIN_MAX
            MaxSdkCallbacks.Rewarded.OnAdReceivedRewardEvent -= OnRewardedAdReceivedReward;
            MaxSdkCallbacks.Rewarded.OnAdDisplayFailedEvent -= OnRewardedAdDisplayFailed;
            MaxSdkCallbacks.Rewarded.OnAdHiddenEvent -= OnRewardedAdHidden;
#endif
        }
    }
}