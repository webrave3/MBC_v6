// /Assets/Project/Scripts/World/WorldSettings.cs
using UnityEngine;

[CreateAssetMenu(fileName = "NewWorldSettings", menuName = "AutoForge/World Settings")]
public class WorldSettings : ScriptableObject
{
    [Header("Chunk Settings")]
    public int chunkSize = 100; // Size of one chunk in world units (e.g., 100x100)
    public int chunkHeight = 50; // Max possible height of terrain
    public int viewDistance = 5; // In chunks (e.g., 5 = 11x11 grid)

    [Header("Prefabs")]
    public GameObject chunkPrefab;

    // We can add noise settings, biome settings, etc. here later
}