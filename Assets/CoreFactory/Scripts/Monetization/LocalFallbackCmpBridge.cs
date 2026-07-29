using CoreFactory.Core;

namespace CoreFactory.Monetization
{
    public class LocalFallbackCmpBridge : IMonetizationCmpBridge
    {
        private System.Action<ConsentStatus> _initializationCallback;
        private System.Action<ConsentStatus> _privacyOptionsCallback;

        public void Initialize(System.Action<ConsentStatus> onComplete)
        {
            _initializationCallback = onComplete;
            EventBus.Subscribe<ConsentPromptDecisionEvent>(OnConsentPromptDecision);
            EventBus.Publish(new ConsentPromptRequestedEvent());
        }

        public void ShowPrivacyOptions(System.Action<ConsentStatus> onComplete)
        {
            _privacyOptionsCallback = onComplete;
            EventBus.Subscribe<ConsentPromptDecisionEvent>(OnConsentPromptDecision);
            EventBus.Publish(new ConsentPromptRequestedEvent());
        }

        private void OnConsentPromptDecision(ConsentPromptDecisionEvent ev)
        {
            EventBus.Unsubscribe<ConsentPromptDecisionEvent>(OnConsentPromptDecision);
            
            if (_initializationCallback != null)
            {
                var cb = _initializationCallback;
                _initializationCallback = null;
                cb.Invoke(ev.Decision);
            }
            else if (_privacyOptionsCallback != null)
            {
                var cb = _privacyOptionsCallback;
                _privacyOptionsCallback = null;
                cb.Invoke(ev.Decision);
            }
        }
    }

    public struct ConsentPromptDecisionEvent
    {
        public ConsentStatus Decision { get; }
        public ConsentPromptDecisionEvent(ConsentStatus decision) => Decision = decision;
    }
}