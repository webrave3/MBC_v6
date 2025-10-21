// /Assets/Project/Scripts/Core/FactoryManager.cs
using UnityEngine;
using System.Collections.Generic; // Required for List
using AutoForge.Factory; // <-- ADD THIS to recognize the MobileFactory type in its namespace

namespace AutoForge.Core // Assuming FactoryManager is in the Core namespace
{
    /// <summary>
    /// Manages the player's mobile factory instance.
    /// Provides a central point of access (Singleton).
    /// </summary>
    public class FactoryManager : MonoBehaviour
    {
        // --- Singleton Pattern ---
        private static FactoryManager _instance;
        public static FactoryManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<FactoryManager>();
                    if (_instance == null)
                    {
                        GameObject singletonObject = new GameObject("FactoryManager");
                        _instance = singletonObject.AddComponent<FactoryManager>();
                        Debug.LogWarning("FactoryManager instance was not found in the scene. Created one.");
                    }
                }
                return _instance;
            }
        }
        // --- End Singleton ---

        [Header("Factory References")]
        [Tooltip("Direct reference to the player's Mobile Factory instance in the scene.")]
        // We will manage this via registration instead of direct assignment if multiple factories could exist
        // public MobileFactory playerFactory; // Commented out - using the list below

        // --- Use a List to track registered factories ---
        // This is more flexible if you ever have more than one (e.g., enemy factories later)
        private List<MobileFactory> activeFactories = new List<MobileFactory>();

        // --- Property to easily get the primary player factory (assuming only one for now) ---
        public MobileFactory PlayerFactory
        {
            get
            {
                if (activeFactories.Count > 0)
                {
                    return activeFactories[0]; // Return the first registered factory as the player's
                }
                return null; // No factory registered
            }
        }


        private void Awake()
        {
            // --- Singleton Enforcement ---
            if (_instance != null && _instance != this)
            {
                Debug.LogWarning("Duplicate FactoryManager instance found. Destroying self.", this.gameObject);
                Destroy(this.gameObject);
                return;
            }
            _instance = this;
            // Optional: Keep the manager alive across scene loads if needed
            // DontDestroyOnLoad(this.gameObject);
            // --- End Singleton ---
        }

        /// <summary>
        /// Registers a MobileFactory instance with the manager.
        /// Called by MobileFactory in its Awake() method.
        /// </summary>
        /// <param name="factoryToAdd">The MobileFactory instance to register.</param>
        public void RegisterFactory(MobileFactory factoryToAdd) // Parameter type uses the namespace
        {
            if (factoryToAdd == null)
            {
                Debug.LogError("[FactoryManager] Attempted to register a null factory!", this);
                return;
            }

            if (!activeFactories.Contains(factoryToAdd))
            {
                activeFactories.Add(factoryToAdd);
                Debug.Log($"<color=green>[FactoryManager]</color> Registered factory: {factoryToAdd.name}", factoryToAdd);

                // Optional: If you only ever want one player factory, enforce it
                // if (activeFactories.Count > 1) {
                //     Debug.LogWarning("[FactoryManager] More than one MobileFactory registered. This might indicate an issue if only one player factory is expected.", this);
                // }
            }
            else
            {
                Debug.LogWarning($"[FactoryManager] Factory {factoryToAdd.name} is already registered.", factoryToAdd);
            }
        }

        /// <summary>
        /// Unregisters a MobileFactory instance from the manager.
        /// Called by MobileFactory in its OnDestroy() method.
        /// </summary>
        /// <param name="factoryToRemove">The MobileFactory instance to unregister.</param>
        public void UnregisterFactory(MobileFactory factoryToRemove) // Parameter type uses the namespace
        {
            if (factoryToRemove == null)
            {
                Debug.LogWarning("[FactoryManager] Attempted to unregister a null factory!", this);
                return;
            }

            if (activeFactories.Contains(factoryToRemove))
            {
                activeFactories.Remove(factoryToRemove);
                Debug.Log($"<color=orange>[FactoryManager]</color> Unregistered factory: {factoryToRemove.name}", factoryToRemove);
            }
            else
            {
                Debug.LogWarning($"[FactoryManager] Attempted to unregister factory {factoryToRemove.name} which was not registered.", factoryToRemove);
            }
        }

        // --- Example Usage (Optional) ---
        // You could add methods here to control the factory if needed, e.g.:
        // public void CommandFactoryToMove(Vector3 position)
        // {
        //     if (PlayerFactory != null)
        //     {
        //         // PlayerFactory.GoToPosition(position); // Assuming MobileFactory has such a method
        //     }
        // }

        // public void RecallPlayerFactory()
        // {
        //      if (PlayerFactory != null && PlayerController.Instance != null) // Assuming a PlayerController singleton
        //      {
        //           PlayerFactory.WarpToPosition(PlayerController.Instance.transform.position + Vector3.up); // Example recall
        //      }
        // }
        // --- End Example Usage ---
    }
}