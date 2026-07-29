using NUnit.Framework;
using UnityEngine;
using CoreFactory.Core;
using CoreFactory.Monetization;
using System.Reflection;

namespace CoreFactory.Tests.EditMode
{
    [TestFixture]
    public class AdConsentIntegrationTest
    {
        private GameObject _adManagerObject;
        private AdManager _adManager;

        private const string ConsentPrefsKey = "PrivacyConsent_GDPR";
        private const string ConsentTimestampKey = "PrivacyConsent_UtcTicks";
        private const string ConsentPolicyVersKey = "PrivacyConsent_PolicyVersion";
        private int _backupConsent;
        private bool _hadBackup;

        [SetUp]
        public void SetUp()
        {
            _hadBackup = PlayerPrefs.HasKey(ConsentPrefsKey);
            if (_hadBackup) _backupConsent = PlayerPrefs.GetInt(ConsentPrefsKey);

            PlayerPrefs.DeleteKey(ConsentPrefsKey);
            PlayerPrefs.DeleteKey(ConsentTimestampKey);
            PlayerPrefs.DeleteKey(ConsentPolicyVersKey);

            EventBus.Clear();

            _adManagerObject = new GameObject("AdManager_TestContainer");
            _adManager = _adManagerObject.AddComponent<AdManager>();

            FieldInfo instanceField = typeof(Singleton<AdManager>)
                .GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic);

            Assert.IsNotNull(instanceField, "Singleton<T>._instance field missing.");
            instanceField.SetValue(null, _adManager);

            _adManager.Initialize();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_adManagerObject);

            typeof(Singleton<AdManager>)
                .GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic)
                ?.SetValue(null, null);

            EventBus.Clear();

            PlayerPrefs.DeleteKey(ConsentTimestampKey);
            PlayerPrefs.DeleteKey(ConsentPolicyVersKey);
            if (_hadBackup) PlayerPrefs.SetInt(ConsentPrefsKey, _backupConsent);
            else PlayerPrefs.DeleteKey(ConsentPrefsKey);
            PlayerPrefs.Save();
        }

        private void FastForwardSession(float seconds)
        {
            float now = Time.realtimeSinceStartup;
            SetPrivateField("_sessionStartTime", now - seconds);
            SetPrivateField("_lastInterstitialTime", now - seconds);
        }

        private void SetPrivateField(string name, object value)
        {
            FieldInfo f = typeof(AdManager).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(f, $"AdManager.{name} field missing.");
            f.SetValue(_adManager, value);
        }

        [Test]
        public void TryShowInterstitial_ConsentDenied_NoNpaFallback_ReturnsFalse()
        {
            SetPrivateField("allowNonPersonalizedAdsWhenDenied", false);
            _adManager.DenyConsent();
            FastForwardSession(300f);
            Assert.IsFalse(_adManager.TryShowInterstitial("Test"));
        }

        [Test]
        public void TryShowInterstitial_ConsentDenied_WithNpaFallback_ReturnsTrue()
        {
            // Default behaviour check (allowNonPersonalizedAdsWhenDenied is true by default)
            SetPrivateField("allowNonPersonalizedAdsWhenDenied", true);
            _adManager.DenyConsent();
            FastForwardSession(300f);
            Assert.IsTrue(_adManager.TryShowInterstitial("Test"), "Serving Non-Personalized Ad (NPA) failed when consent was denied.");
        }

        [Test]
        public void TryShowInterstitial_ConsentGranted_AfterSessionDelay_ReturnsTrue()
        {
            _adManager.GrantConsent();
            FastForwardSession(300f);
            Assert.IsTrue(_adManager.TryShowInterstitial("Test"));
        }

        [Test]
        public void TryShowInterstitial_IsIndependentOfMachineLocale()
        {
            _adManager.GrantConsent();
            FastForwardSession(300f);

            FieldInfo cooldownField = typeof(AdManager).GetField("_activeCooldown", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(cooldownField);

            foreach (float cooldown in new[] { 60f, 120f })
            {
                cooldownField.SetValue(_adManager, cooldown);
                FastForwardSession(300f);
                Assert.IsTrue(_adManager.TryShowInterstitial("LocaleTest"));
            }
        }

        [Test]
        public void Awake_SeedsLastInterstitialAfterCooldownIsResolved()
        {
            FieldInfo cooldownField = typeof(AdManager).GetField("_activeCooldown", BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo lastAdField = typeof(AdManager).GetField("_lastInterstitialTime", BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo sessionField = typeof(AdManager).GetField("_sessionStartTime", BindingFlags.Instance | BindingFlags.NonPublic);

            float cooldown = (float)cooldownField.GetValue(_adManager);
            float lastAd = (float)lastAdField.GetValue(_adManager);
            float session = (float)sessionField.GetValue(_adManager);

            Assert.AreEqual(session - cooldown, lastAd, 0.01f);
        }

        [Test]
        public void ConsentPromptEvent_HasAtLeastOneListener()
        {
            bool received = false;
            System.Action<ConsentPromptRequestedEvent> handler = _ => received = true;
            EventBus.Subscribe(handler);
            int delivered = EventBus.Publish(new ConsentPromptRequestedEvent());
            EventBus.Unsubscribe(handler);

            Assert.AreEqual(1, delivered);
            Assert.IsTrue(received);
        }

        [Test]
        public void StickyConsentEvent_IsReplayedToLateSubscriber()
        {
            EventBus.MarkSticky<ConsentPromptRequestedEvent>();
            EventBus.Publish(new ConsentPromptRequestedEvent());

            bool receivedLate = false;
            System.Action<ConsentPromptRequestedEvent> late = _ => receivedLate = true;
            EventBus.Subscribe(late);
            EventBus.Unsubscribe(late);
            EventBus.ClearSticky<ConsentPromptRequestedEvent>();

            Assert.IsTrue(receivedLate);
        }

        [Test]
        public void Interstitial_RespectsCooldownBetweenTwoCalls()
        {
            _adManager.GrantConsent();
            FastForwardSession(300f);
            Assert.IsTrue(_adManager.TryShowInterstitial("First"));
            Assert.IsFalse(_adManager.TryShowInterstitial("Second"));
        }

        [Test]
        public void RewardedAd_WithoutConsent_InvokesFailureCallback()
        {
            SetPrivateField("allowNonPersonalizedAdsWhenDenied", false);
            _adManager.DenyConsent();

            bool rewarded = false;
            bool failed = false;

            _adManager.ShowRewardedAd("Revive",
                onRewardEarned: () => rewarded = true,
                onFailed: _ => failed = true);

            Assert.IsFalse(rewarded);
            Assert.IsTrue(failed);
        }

        [Test]
        public void RewardedAd_WithConsent_ButNoSdk_DoesNotGrantReward()
        {
            // Verifies the #else compiler branches cleanly prevents free rewards (LEG-07 fix!)
            _adManager.GrantConsent();
            bool rewarded = false;
            bool failed = false;

            _adManager.ShowRewardedAd("Revive",
                onRewardEarned: () => rewarded = true,
                onFailed: _ => failed = true);

            Assert.IsFalse(rewarded, "LEG-07: Reward granted despite SDK being completely missing!");
            Assert.IsTrue(failed, "Failure callback was not triggered under unsupported/editor targets.");
        }

        [Test]
        public void ConsentRecord_IsAuditable()
        {
            _adManager.GrantConsent();
            Assert.IsTrue(PlayerPrefs.HasKey(ConsentTimestampKey));
            Assert.IsTrue(PlayerPrefs.HasKey(ConsentPolicyVersKey));
        }

        [Test]
        public void TamperedConsentValue_FallsBackToNotDetermined()
        {
            PlayerPrefs.SetInt(ConsentPrefsKey, 9999);
            PlayerPrefs.SetInt(ConsentPolicyVersKey, 1);
            PlayerPrefs.Save();

            var obj = new GameObject("Tampered");
            var mgr = obj.AddComponent<AdManager>();
            mgr.Initialize();

            Assert.AreEqual(ConsentStatus.NotDetermined, mgr.ConsentState);
            Object.DestroyImmediate(obj);
        }
    }
}