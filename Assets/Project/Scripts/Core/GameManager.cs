using UnityEngine;

namespace AutoForge.Core
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public bool IsPlayerInUIMode { get; private set; }

        public enum GameState
        {
            Playing,
            Paused,
            GameOver
        }
        public GameState CurrentState { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        void Start()
        {
            ChangeState(GameState.Playing);
            SetPlayerInUIMode(false);
        }

        public void SetPlayerInUIMode(bool isInUI)
        {
            IsPlayerInUIMode = isInUI;
        }

        public void ChangeState(GameState newState)
        {
            if (CurrentState == newState) return;
            CurrentState = newState;
        }
    }
}

