using UnityEngine;
using UnityEngine.InputSystem;
using AutoForge.UI;
using AutoForge.Core;

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

            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetPlayerInUIMode(isInventoryOpen);
            }
        }

        private void SetCursorState(bool uiModeEnabled)
        {
            Cursor.lockState = uiModeEnabled ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = uiModeEnabled;
            // No longer pausing the game
            Time.timeScale = 1f;
        }
    }
}

