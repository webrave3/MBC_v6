using UnityEngine;
using AutoForge.Core;

[CreateAssetMenu(fileName = "New Building Data", menuName = "AutoForge/Building Data")]
public class BuildingData : ScriptableObject
{
    [Header("Building Identity")]
    public string buildingName;
    public GameObject buildingPrefab;

    [Header("UI")]
    public Sprite buildingIcon;

    [Header("Construction")]
    public ResourceType costType;
    public int costAmount;

    [Header("Functionality (Optional)")]
    [Tooltip("Used by booster buildings. Set to 0 for non-boosters.")]
    public float damageBonus = 0f;
}

