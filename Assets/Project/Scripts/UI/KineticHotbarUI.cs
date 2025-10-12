using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using AutoForge.Core;

namespace AutoForge.UI
{
    public class KineticHotbarUI : MonoBehaviour
    {
        public static KineticHotbarUI Instance { get; private set; }

        [Header("Prefab Reference")]
        [SerializeField] private GameObject buildSlotPrefab;

        private GameObject actionHotbarPanel;
        private GameObject buildHotbarPanel;
        private Transform buildSlotsParent;
        private List<GameObject> activeSlots = new List<GameObject>();

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            actionHotbarPanel = transform.Find("ActionHotbarPanel")?.gameObject;
            buildHotbarPanel = transform.Find("BuildHotbarPanel")?.gameObject;
            if (buildHotbarPanel != null) buildSlotsParent = buildHotbarPanel.transform;

            if (actionHotbarPanel != null) actionHotbarPanel.SetActive(true);
            if (buildHotbarPanel != null) buildHotbarPanel.SetActive(false);
        }

        public void ShowActionMode()
        {
            if (actionHotbarPanel == null || buildHotbarPanel == null) return;
            actionHotbarPanel.SetActive(true);
            buildHotbarPanel.SetActive(false);
        }

        public void ShowBuildTier1(List<BuildCategory> categories)
        {
            if (buildHotbarPanel == null || buildSlotPrefab == null) return;
            actionHotbarPanel.SetActive(false);
            buildHotbarPanel.SetActive(true);
            ClearSlots();

            for (int i = 0; i < categories.Count; i++)
            {
                GameObject slotGO = Instantiate(buildSlotPrefab, buildSlotsParent);
                BuildSlotUI slotUI = slotGO.GetComponent<BuildSlotUI>();

                if (slotUI != null)
                {
                    slotUI.labelText.text = $"[{i + 1}] {categories[i].categoryName}";
                    slotUI.iconImage.enabled = false;
                }
                activeSlots.Add(slotGO);
            }
        }

        public void ShowBuildTier2(BuildCategory category)
        {
            if (buildHotbarPanel == null || buildSlotPrefab == null) return;
            actionHotbarPanel.SetActive(false);
            buildHotbarPanel.SetActive(true);
            ClearSlots();
            if (category == null) return;

            for (int i = 0; i < category.buildingsInCategory.Count; i++)
            {
                GameObject slotGO = Instantiate(buildSlotPrefab, buildSlotsParent);
                BuildSlotUI slotUI = slotGO.GetComponent<BuildSlotUI>();
                BuildingData data = category.buildingsInCategory[i];

                if (slotUI != null)
                {
                    // --- UPDATED LOGIC ---
                    // Display the building's actual name and icon
                    slotUI.labelText.text = data.buildingName;
                    slotUI.iconImage.sprite = data.buildingIcon;
                    slotUI.iconImage.enabled = true;
                    // --- END UPDATED LOGIC ---
                }
                activeSlots.Add(slotGO);
            }
        }

        private void ClearSlots()
        {
            foreach (GameObject slot in activeSlots)
            {
                Destroy(slot);
            }
            activeSlots.Clear();
        }
    }
}