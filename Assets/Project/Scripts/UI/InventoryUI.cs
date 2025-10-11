using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem; // <-- 1. ADD THIS to use the new Input System
using TMPro;
using AutoForge.Player;
using AutoForge.Core;

namespace AutoForge.UI
{
    public class InventoryUI : MonoBehaviour
    {
        public static InventoryUI Instance { get; private set; }

        [Header("UI Components")]
        [SerializeField] private GameObject inventoryPanel;
        [SerializeField] private Transform slotsParent;
        [SerializeField] private GameObject slotPrefab;
        [SerializeField] private Image dragIcon;

        private class InventorySlotUI
        {
            public Image itemIcon;
            public TextMeshProUGUI amountText;
        }
        private List<InventorySlotUI> slotUIs = new List<InventorySlotUI>();

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            if (PlayerInventory.Instance != null)
            {
                PlayerInventory.Instance.OnInventoryChanged += UpdateUI;
            }
            InitializeInventory();
            inventoryPanel.SetActive(false);
            if (dragIcon != null) dragIcon.enabled = false;
        }

        private void OnDestroy()
        {
            if (PlayerInventory.Instance != null)
            {
                PlayerInventory.Instance.OnInventoryChanged -= UpdateUI;
            }
        }

        void Update()
        {
            if (dragIcon != null && dragIcon.enabled)
            {
                // --- 2. THIS IS THE FIX for the mouse position error ---
                dragIcon.transform.position = Mouse.current.position.ReadValue();
            }
        }

        public void ToggleInventoryPanel(bool show)
        {
            inventoryPanel.SetActive(show);
        }

        private void InitializeInventory()
        {
            if (PlayerInventory.Instance == null) return;
            foreach (Transform child in slotsParent) Destroy(child.gameObject);
            slotUIs.Clear();

            for (int i = 0; i < PlayerInventory.Instance.inventorySize; i++)
            {
                GameObject slotGO = Instantiate(slotPrefab, slotsParent);
                if (slotGO.TryGetComponent<InventorySlot>(out var slotScript))
                {
                    slotScript.slotIndex = i;
                }
                InventorySlotUI newSlot = new InventorySlotUI
                {
                    itemIcon = slotGO.transform.Find("ItemIcon").GetComponent<Image>(),
                    amountText = slotGO.transform.Find("AmountText").GetComponent<TextMeshProUGUI>()
                };
                slotUIs.Add(newSlot);
            }
            UpdateUI();
        }

        public void StartDrag(int slotIndex)
        {
            if (PlayerInventory.Instance.items[slotIndex] != null && dragIcon != null)
            {
                dragIcon.sprite = PlayerInventory.Instance.items[slotIndex].itemType.resourceIcon;

                // --- 3. THIS IS THE FIX for the invisible drag icon ---
                dragIcon.color = Color.white; // Force it to be fully visible
                dragIcon.enabled = true;
            }
        }

        public void EndDrag()
        {
            if (dragIcon != null) dragIcon.enabled = false;
        }

        private void UpdateUI()
        {
            if (PlayerInventory.Instance == null) return;
            List<InventoryItem> items = PlayerInventory.Instance.items;
            for (int i = 0; i < slotUIs.Count; i++)
            {
                if (i < items.Count && items[i] != null && items[i].amount > 0)
                {
                    slotUIs[i].itemIcon.sprite = items[i].itemType.resourceIcon;
                    slotUIs[i].itemIcon.enabled = true;
                    if (items[i].amount > 1)
                    {
                        slotUIs[i].amountText.text = items[i].amount.ToString();
                        slotUIs[i].amountText.enabled = true;
                    }
                    else
                    {
                        slotUIs[i].amountText.enabled = false;
                    }
                }
                else
                {
                    slotUIs[i].itemIcon.sprite = null;
                    slotUIs[i].itemIcon.enabled = false;
                    slotUIs[i].amountText.enabled = false;
                }
            }
        }
    }
}

