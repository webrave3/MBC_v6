using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using AutoForge.Core;
using AutoForge.Player;

namespace AutoForge.UI
{
    public class HotbarManager : MonoBehaviour
    {
        // --- SINGLETON PATTERN ---
        public static HotbarManager Instance { get; private set; }
        // -------------------------

        private enum HotbarState { Action, Build_Tier1, Build_Tier2 }
        private HotbarState currentState;

        [Header("Data References")]
        [SerializeField] private List<BuildCategory> buildCategories = new List<BuildCategory>();

        private PlayerBuilder playerBuilder;
        private KineticHotbarUI hotbarUI;
        private BuildCategory currentBuildCategory;

        private void Awake()
        {
            // --- SINGLETON SETUP ---
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            // ---------------------

            playerBuilder = GetComponent<PlayerBuilder>();

            // The Singleton for KineticHotbarUI is set in its own Awake,
            // so we can safely grab it here.
            hotbarUI = KineticHotbarUI.Instance;

            if (playerBuilder == null) Debug.LogError("HotbarManager Error: PlayerBuilder component not found on the same GameObject.", this);
            if (hotbarUI == null) Debug.LogError("HotbarManager Error: Could not find an active KineticHotbarUI instance in the scene! Ensure the HotbarUI_Controller is in the scene and enabled.", this);
        }

        private void Start()
        {
            ChangeState(HotbarState.Action);
        }

        public void OnToggleBuildMode(InputValue value)
        {
            if (value.isPressed)
            {
                if (currentState == HotbarState.Action)
                {
                    ChangeState(HotbarState.Build_Tier1);
                }
                else
                {
                    ChangeState(HotbarState.Action);
                }
            }
        }

        public void OnHotbarBack(InputValue value)
        {
            if (value.isPressed && currentState == HotbarState.Build_Tier2)
            {
                ChangeState(HotbarState.Build_Tier1);
            }
        }

        public void OnHotbarSelect1(InputValue value) => HandleHotbarSelection(0);
        public void OnHotbarSelect2(InputValue value) => HandleHotbarSelection(1);
        public void OnHotbarSelect3(InputValue value) => HandleHotbarSelection(2);
        public void OnHotbarSelect4(InputValue value) => HandleHotbarSelection(3);

        private void HandleHotbarSelection(int index)
        {
            if (currentState == HotbarState.Build_Tier1)
            {
                if (index < buildCategories.Count)
                {
                    currentBuildCategory = buildCategories[index];
                    ChangeState(HotbarState.Build_Tier2);
                }
            }
            else if (currentState == HotbarState.Build_Tier2)
            {
                if (currentBuildCategory != null && index < currentBuildCategory.buildingsInCategory.Count)
                {
                    BuildingData selectedBuilding = currentBuildCategory.buildingsInCategory[index];
                    playerBuilder.SetBuildingToPlace(selectedBuilding);
                    ChangeState(HotbarState.Action); // Switch back to action mode to place the item
                }
            }
        }

        private void ChangeState(HotbarState newState)
        {
            if (hotbarUI == null) return; // Safety check

            currentState = newState;
            Debug.Log("<color=cyan>[HotbarManager]</color> State changed to: " + currentState);

            bool isInUI = (currentState != HotbarState.Action);
            GameManager.Instance.SetPlayerInUIMode(isInUI);

            // Only tell the builder to cancel if we are explicitly returning to Action mode
            if (!isInUI)
            {
                playerBuilder.CancelBuildMode();
            }

            switch (currentState)
            {
                case HotbarState.Action:
                    hotbarUI.ShowActionMode();
                    break;
                case HotbarState.Build_Tier1:
                    hotbarUI.ShowBuildTier1(buildCategories);
                    break;
                case HotbarState.Build_Tier2:
                    hotbarUI.ShowBuildTier2(currentBuildCategory);
                    break;
            }
        }
    }
}

