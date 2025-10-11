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
            // Ensure the game starts in the correct state
            SetPlayerInUIMode(false);
        }

        public void SetPlayerInUIMode(bool isInUI)
        {
            IsPlayerInUIMode = isInUI;
            Cursor.lockState = isInUI ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = isInUI;
        }

        public void ChangeState(GameState newState)
        {
            if (CurrentState == newState) return;
            CurrentState = newState;
        }
    }
}