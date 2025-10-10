using UnityEngine;

namespace AutoForge.Core
{
    [CreateAssetMenu(fileName = "New ResourceType", menuName = "AutoForge/ResourceType")]
    public class ResourceType : ScriptableObject
    {
        [Header("Resource Details")]
        public string resourceName = "New Resource";
        public Sprite resourceIcon;
        [TextArea]
        public string description;
    }
}