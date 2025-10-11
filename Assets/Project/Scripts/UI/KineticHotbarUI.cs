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
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // --- THE FINAL FOOLPROOF FIX ---
            // This correctly looks for DIRECT CHILDREN of this GameObject.
            actionHotbarPanel = transform.Find("ActionHotbarPanel")?.gameObject;
            buildHotbarPanel = transform.Find("BuildHotbarPanel")?.gameObject;
            // --- END FIX ---

            if (actionHotbarPanel == null) Debug.LogError("KineticHotbarUI Error: Could not find a child GameObject named 'ActionHotbarPanel'. It must be a direct child of this object.", this);
            if (buildHotbarPanel == null) Debug.LogError("KineticHotbarUI Error: Could not find a child GameObject named 'BuildHotbarPanel'. It must be a direct child of this object.", this);
            else
            {
                buildSlotsParent = buildHotbarPanel.transform;
            }

            if (buildSlotPrefab == null) Debug.LogError("KineticHotbarUI Error: The 'Build Slot Prefab' has not been assigned in the Inspector! Please drag it from the Project folder.", this);

            if (actionHotbarPanel != null) actionHotbarPanel.SetActive(false);
            if (buildHotbarPanel != null) buildHotbarPanel.SetActive(false);
        }

        private void Start()
        {
            // Set the initial state correctly
            if (actionHotbarPanel != null)
            {
                ShowActionMode();
            }
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
                slotGO.GetComponentInChildren<TextMeshProUGUI>().text = $"[{i + 1}] {categories[i].categoryName}";

                Image icon = slotGO.transform.Find("Icon")?.GetComponent<Image>();
                if (icon != null) icon.enabled = false;

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
                BuildingData data = category.buildingsInCategory[i];

                slotGO.GetComponentInChildren<TextMeshProUGUI>().text = $"[{i + 1}]";
                Image icon = slotGO.transform.Find("Icon")?.GetComponent<Image>();
                if (icon != null)
                {
                    icon.sprite = data.buildingIcon;
                    icon.enabled = true;
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

