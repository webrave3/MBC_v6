using UnityEngine;

namespace AutoForge.Core
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        // --- NEW CODE ---
        // This flag will be true whenever a full-screen UI is open
        public bool IsPlayerInUIMode { get; private set; }
        // --- END NEW CODE ---

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
            // Ensure we start not in UI mode
            SetPlayerInUIMode(false);
        }

        // --- NEW METHOD ---
        public void SetPlayerInUIMode(bool isInUI)
        {
            IsPlayerInUIMode = isInUI;
        }
        // --- END NEW METHOD ---

        public void ChangeState(GameState newState)
        {
            if (CurrentState == newState) return;
            CurrentState = newState;
            Debug.Log("Game state changed to: " + newState);
        }
    }
}
