using System;
using UnityEngine;

namespace CoreFactory.Core
{
    public enum GamePhase { Splash, Menu, Playing, Paused, GameOver, Results }

    public class StateManager : Singleton<StateManager>
    {
        public event Action<GamePhase> OnPhaseChanged;
        [SerializeField] private GamePhase currentPhase = GamePhase.Splash;
        public GamePhase CurrentPhase => currentPhase;

        public void TransitionTo(GamePhase nextPhase)
        {
            if (currentPhase == nextPhase) return;
            currentPhase = nextPhase;
            OnPhaseChanged?.Invoke(currentPhase);
            EventBus.Publish(new PhaseTransitionEvent(nextPhase));
        }
    }

    public struct PhaseTransitionEvent
    {
        public GamePhase NewPhase { get; }
        public PhaseTransitionEvent(GamePhase phase) => NewPhase = phase;
    }
}