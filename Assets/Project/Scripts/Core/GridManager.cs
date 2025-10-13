using UnityEngine;
using System.Collections.Generic;

public class GridManager : MonoBehaviour
{
    public static GridManager Instance { get; private set; }

    public List<FactoryTile> AllTiles { get; private set; } = new List<FactoryTile>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void RegisterTile(FactoryTile tile)
    {
        if (!AllTiles.Contains(tile))
        {
            AllTiles.Add(tile);
        }
    }
}