using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using AutoForge.UI;
using AutoForge.Player;

namespace AutoForge.Core
{
    public class HotbarManager : MonoBehaviour
    {
        public static HotbarManager Instance { get; private set; }

        [Header("Build Categories")]
        [SerializeField] private List<BuildCategory> buildCategoriesTier1;

        private enum BuildState { None, Tier1_Categories, Tier2_Buildings }
        private BuildState currentBuildState = BuildState.None;
        private BuildCategory selectedCategory;
        private bool isBuildModeActive = false;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        // --- INPUT SYSTEM HOOKS (Called by SendMessage) ---

        public void OnBuildMode(InputValue value)
        {
            if (value.isPressed) ToggleBuildMode();
        }

        public void OnBuildSlot1(InputValue value) { if (value.isPressed) OnHotbarInput(0); }
        public void OnBuildSlot2(InputValue value) { if (value.isPressed) OnHotbarInput(1); }
        public void OnBuildSlot3(InputValue value) { if (value.isPressed) OnHotbarInput(2); }
        public void OnBuildSlot4(InputValue value) { if (value.isPressed) OnHotbarInput(3); }

        public void OnBuildBack(InputValue value)
        {
            if (value.isPressed && isBuildModeActive)
            {
                NavigateBack();
            }
        }

        // --- INTERNAL LOGIC ---

        private void ToggleBuildMode()
        {
            isBuildModeActive = !isBuildModeActive;

            if (isBuildModeActive)
            {
                currentBuildState = BuildState.Tier1_Categories;
                KineticHotbarUI.Instance.ShowBuildTier1(buildCategoriesTier1);
            }
            else
            {
                currentBuildState = BuildState.None;
                KineticHotbarUI.Instance.ShowActionMode();

                if (PlayerBuilder.Instance != null)
                {
                    PlayerBuilder.Instance.CancelBuildMode();
                }
            }
        }

        private void OnHotbarInput(int slotIndex)
        {
            if (!isBuildModeActive) return;

            if (currentBuildState == BuildState.Tier1_Categories)
            {
                if (slotIndex < buildCategoriesTier1.Count)
                {
                    selectedCategory = buildCategoriesTier1[slotIndex];
                    currentBuildState = BuildState.Tier2_Buildings;
                    KineticHotbarUI.Instance.ShowBuildTier2(selectedCategory);
                }
            }
            else if (currentBuildState == BuildState.Tier2_Buildings)
            {
                if (selectedCategory != null && slotIndex < selectedCategory.buildingsInCategory.Count)
                {
                    BuildingData selectedBuilding = selectedCategory.buildingsInCategory[slotIndex];
                    if (PlayerBuilder.Instance != null)
                    {
                        PlayerBuilder.Instance.SelectBuildingToPlace(selectedBuilding);
                    }
                }
            }
        }

        private void NavigateBack()
        {
            if (PlayerBuilder.Instance != null)
            {
                PlayerBuilder.Instance.CancelBuildMode();
            }

            if (currentBuildState == BuildState.Tier2_Buildings)
            {
                currentBuildState = BuildState.Tier1_Categories;
                KineticHotbarUI.Instance.ShowBuildTier1(buildCategoriesTier1);
            }
            else if (currentBuildState == BuildState.Tier1_Categories)
            {
                ToggleBuildMode();
            }
        }
    }
}