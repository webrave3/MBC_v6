using UnityEngine;
using UnityEngine.InputSystem;
using AutoForge.UI;
using AutoForge.Core; // To access the GameManager

namespace AutoForge.Player
{
    public class UIInputReceiver : MonoBehaviour
    {
        private InventoryUI inventoryUI;
        private bool isInventoryOpen = false;

        void Start()
        {
            inventoryUI = FindObjectOfType<InventoryUI>();
            SetCursorState(false);
        }

        public void OnToggleInventory(InputValue value)
        {
            if (inventoryUI == null) return;

            isInventoryOpen = !isInventoryOpen;

            inventoryUI.ToggleInventoryPanel(isInventoryOpen);
            SetCursorState(isInventoryOpen);

            // --- NEW ---
            // Tell the GameManager about the new state
            GameManager.Instance.SetPlayerInUIMode(isInventoryOpen);
        }

        private void SetCursorState(bool uiModeEnabled)
        {
            if (uiModeEnabled)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                // --- REMOVED ---
                // Time.timeScale = 0f; // We no longer pause the game
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                // --- CHANGED ---
                Time.timeScale = 1f; // Ensure game is unpaused when closing UI
            }
        }
    }
}

