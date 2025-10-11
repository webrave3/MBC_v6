using System.Collections.Generic;
using UnityEngine;

namespace AutoForge.Core
{
    [CreateAssetMenu(fileName = "New Build Category", menuName = "AutoForge/Build Category")]
    public class BuildCategory : ScriptableObject
    {
        [Tooltip("The name displayed on the hotbar for this category (e.g., 'Defense').")]
        public string categoryName;

        [Tooltip("The list of building items that belong to this category.")]
        public List<BuildingData> buildingsInCategory;
    }
}

