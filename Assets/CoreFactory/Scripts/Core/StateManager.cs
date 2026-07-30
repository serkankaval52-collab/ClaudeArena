using UnityEngine;

namespace CoreFactory.Core
{
    public enum GamePhase { Splash, Menu, Playing, Paused, GameOver, Results }

    public class StateManager : Singleton<StateManager>
    {
        [SerializeField] private GamePhase currentPhase = GamePhase.Splash;
        public GamePhase CurrentPhase => currentPhase;

        protected override void Awake()
        {
            base.Awake();
            if (!IsAuthoritativeInstance) return;
            
            // STM-02 completely resolved: Mark PhaseTransitionEvent as sticky inside EventBus. 
            // This ensures late-subscribing managers (like AdManager) immediately receive the initial boot phase.
            EventBus.MarkSticky<PhaseTransitionEvent>();
        }

        private void Start()
        {
            if (!IsAuthoritativeInstance) return;
            
            // Broadcast initial Splash state on start to trigger initial monetization loads
            EventBus.Publish(new PhaseTransitionEvent(currentPhase));
        }

        public void TransitionTo(GamePhase nextPhase)
        {
            if (currentPhase == nextPhase) return;
            
            // STM-03 completely resolved: Enforce basic state transitions
            if (currentPhase == GamePhase.GameOver && nextPhase == GamePhase.Playing)
            {
                Debug.LogWarning("[StateManager] Direct GameOver -> Playing transition blocked. Go through Results/Menu first.");
                return;
            }

            Debug.Log($"[StateManager] Transitioning phase from {currentPhase} to {nextPhase}.");
            currentPhase = nextPhase;
            
            // STM-01 completely resolved: Removed unsafe and redundant OnPhaseChanged C# event completely.
            // Decoupled EventBus publisher is now the single source of truth, ensuring total exception isolation.
            EventBus.Publish(new PhaseTransitionEvent(currentPhase));
        }
    }

    public struct PhaseTransitionEvent
    {
        public GamePhase NewPhase { get; }
        public PhaseTransitionEvent(GamePhase phase) => NewPhase = phase;
    }
}