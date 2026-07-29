namespace CoreFactory.Monetization
{
    public interface IMonetizationCmpBridge
    {
        void Initialize(System.Action<ConsentStatus> onComplete);
        void ShowPrivacyOptions(System.Action<ConsentStatus> onComplete);
    }

    // Consolidated core compliance structures to completely resolve CS0101 & CS0246 (Bug 1 fixed!)
    public struct ConsentPromptRequestedEvent { }

    public struct ConsentStateChangedEvent
    {
        public ConsentStatus NewStatus { get; }
        public ConsentStateChangedEvent(ConsentStatus status) => NewStatus = status;
    }
}