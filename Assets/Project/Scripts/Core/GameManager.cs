// /Assets/Project/Scripts/Core/GameManager.cs
using UnityEngine;
using AutoForge.World; // <-- ADD THIS NAMESPACE

namespace AutoForge.Core
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public bool IsPlayerInUIMode { get; private set; }

        public GameObject mobileFactoryPrefab;

        // --- ADD THIS ---
        [Tooltip("The initial spawn position for the factory. Y-value will be automatically corrected to terrain height.")]
        public Vector3 factorySpawnPosition = new Vector3(0, 0, 5);
        // --- END ADD ---

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

            // --- REMOVE THIS LINE ---
            // Instantiate(mobileFactoryPrefab, new Vector3(0, 0, 5), Quaternion.identity);

            // --- ADD THIS LOGIC ---
            // Wait for the world to be generated before spawning the factory
            if (WorldManager.Instance != null)
            {
                WorldManager.Instance.OnInitialWorldGenerated += SpawnInitialFactory;
            }
            else
            {
                Debug.LogError("<color=red>[GameManager ERROR]</color> WorldManager.Instance is null! Cannot subscribe to world generation event.", this);
            }
            // --- END ADD ---
        }

        // --- ADD THIS METHOD ---
        private void OnDestroy()
        {
            // Ensure we unsubscribe if this object is destroyed
            if (WorldManager.Instance != null)
            {
                WorldManager.Instance.OnInitialWorldGenerated -= SpawnInitialFactory;
            }
        }

        // --- ADD THIS METHOD ---
        private void SpawnInitialFactory()
        {
            // Unsubscribe immediately so this only fires once
            WorldManager.Instance.OnInitialWorldGenerated -= SpawnInitialFactory;

            if (mobileFactoryPrefab == null)
            {
                Debug.LogError("<color=red>[GameManager ERROR]</color> MobileFactoryPrefab is not assigned!", this);
                return;
            }

            // Get a safe spawn position from the WorldManager
            Vector3 safeSpawnPos = WorldManager.Instance.GetSafeSpawnPosition(factorySpawnPosition);

            // Add a small offset so it spawns slightly above ground, not intersecting
            safeSpawnPos.y += 0.1f;

            Debug.Log($"<color=green>[GameManager]</color> Spawning initial factory at safe position: {safeSpawnPos}");
            Instantiate(mobileFactoryPrefab, safeSpawnPos, Quaternion.identity);
        }
        // --- END ADD ---

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