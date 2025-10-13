using UnityEngine;

namespace AutoForge.Core
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public bool IsPlayerInUIMode { get; private set; }

        // Add this line to hold the factory prefab
        public GameObject mobileFactoryPrefab;

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

            // Add this line to spawn the factory at the start of the game
            Instantiate(mobileFactoryPrefab, new Vector3(0, 0, 5), Quaternion.identity);
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