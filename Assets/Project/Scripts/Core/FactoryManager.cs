using UnityEngine;
using System.Collections.Generic;
using AutoForge.Factory; // Namespace for MobileFactory

namespace AutoForge.Core
{
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
                        Debug.LogWarning("FactoryManager instance was not found. Created one.");
                    }
                }
                return _instance;
            }
        }
        // --- End Singleton ---

        // --- Use a List to track registered factories ---
        // **** USES MobileFactory TYPE ****
        private List<MobileFactory> activeFactories = new List<MobileFactory>();

        // --- Property to easily get the primary player factory ---
        // **** USES MobileFactory TYPE ****
        public MobileFactory PlayerFactory
        {
            get
            {
                if (activeFactories.Count > 0)
                {
                    return activeFactories[0]; // Return the first registered factory
                }
                return null; // No factory registered
            }
        }

        private void Awake()
        {
            // --- Singleton Enforcement ---
            if (_instance != null && _instance != this)
            {
                Debug.LogWarning("Duplicate FactoryManager. Destroying self.", gameObject);
                Destroy(gameObject);
                return;
            }
            _instance = this;
            // DontDestroyOnLoad(gameObject); // Optional
        }

        /// <summary>
        /// Registers a MobileFactory instance.
        /// </summary>
        // **** ACCEPTS MobileFactory TYPE ****
        public void RegisterFactory(MobileFactory factoryToAdd)
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
            }
            else
            {
                Debug.LogWarning($"[FactoryManager] Factory {factoryToAdd.name} is already registered.", factoryToAdd);
            }
        }

        /// <summary>
        /// Unregisters a MobileFactory instance.
        /// Should be called from MobileFactory's OnDestroy method.
        /// </summary>
        // **** ACCEPTS MobileFactory TYPE ****
        public void UnregisterFactory(MobileFactory factoryToRemove)
        {
            if (factoryToRemove == null) return; // Don't warn on destroy

            if (activeFactories.Contains(factoryToRemove))
            {
                activeFactories.Remove(factoryToRemove);
                Debug.Log($"<color=orange>[FactoryManager]</color> Unregistered factory: {factoryToRemove.name}", factoryToRemove);
            }
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                activeFactories.Clear();
                _instance = null;
            }
        }
    }
}