using UnityEngine;

namespace AutoForge.Core
{
    /// <summary>
    /// The central brain of the game. Manages game state and provides access to other managers.
    /// This is a Singleton, meaning there is only ever one instance of it.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        // This is the Singleton pattern. It allows any script to access the GameManager
        // by simply typing "GameManager.Instance".
        public static GameManager Instance { get; private set; }

        public enum GameState
        {
            Playing,
            Paused,
            GameOver
        }

        public GameState CurrentState { get; private set; }

        private void Awake()
        {
            // Singleton setup: If an instance already exists, destroy this one.
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // Optional: Keep the GameManager alive when loading new scenes.
            // DontDestroyOnLoad(gameObject);
        }

        void Start()
        {
            // Set the initial game state when the game begins.
            ChangeState(GameState.Playing);
        }

        public void ChangeState(GameState newState)
        {
            if (CurrentState == newState) return;

            CurrentState = newState;
            Debug.Log("Game state changed to: " + newState);

            // Here you can add logic for what happens when a state changes.
            // For example, when changing to Paused, you might set Time.timeScale = 0;
        }
    }
}
