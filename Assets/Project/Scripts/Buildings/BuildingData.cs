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
    [Tooltip("The vertical offset to apply when placing this building to align its base with the ground.")]
    public float placementYOffset = 0f;

    [Header("Stacking Properties")]
    [Tooltip("Can this type of building be stacked on top of another of the same type?")]
    public bool canStack = false; // Added this
    [Tooltip("The vertical distance from one building's base to the next when stacked.")]
    public float stackHeight = 2.0f; // Added this

    [Header("Functionality (Optional)")]
    [Tooltip("Used by booster buildings. Set to 0 for non-boosters.")]
    public float damageBonus = 0f;
}