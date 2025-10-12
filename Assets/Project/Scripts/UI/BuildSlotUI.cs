using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace AutoForge.UI
{
    /// <summary>
    /// A simple helper script attached to the BuildSlotPrefab. It holds direct
    /// references to the UI elements within the prefab to make updating them
    /// clean and reliable.
    /// </summary>
    public class BuildSlotUI : MonoBehaviour
    {
        [Header("UI Element References")]
        [Tooltip("The TextMeshProUGUI element used to display the building's name or category.")]
        public TextMeshProUGUI labelText;

        [Tooltip("The Image element used to display the building's icon.")]
        public Image iconImage;
    }
}