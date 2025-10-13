using UnityEngine;
using UnityEngine.InputSystem;

public class DebugTilePlacer : MonoBehaviour
{
    [Tooltip("Assign your MobileFactory prefab here in the Inspector.")]
    public GameObject mobileFactoryPrefab;

    [Tooltip("Assign your Tile prefab here.")]
    public GameObject tilePrefab;

    [Tooltip("Set this to your 'Socket' layer.")]
    public LayerMask socketLayer;

    private Camera mainCamera;
    private GameObject activeFactory;

    void Start()
    {
        mainCamera = Camera.main;
        // Spawn the factory at the start.
        activeFactory = Instantiate(mobileFactoryPrefab, new Vector3(0, 1, 0), Quaternion.identity);
        Debug.Log("DEBUG: Test factory spawned.");
    }

    void Update()
    {
        // Use the 'F' key for our test placement.
        if (Keyboard.current.fKey.wasPressedThisFrame)
        {
            Debug.Log("DEBUG: 'F' key pressed. Attempting to place tile...");
            TryPlaceTile();
        }
    }

    void TryPlaceTile()
    {
        Ray ray = mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());

        // Draw a visible line in the Scene view to show where we are aiming.
        Debug.DrawRay(ray.origin, ray.direction * 100f, Color.red, 2f);

        if (Physics.Raycast(ray, out RaycastHit hit, 100f, socketLayer))
        {
            Debug.Log($"DEBUG: Raycast HIT! It hit an object named '{hit.collider.name}' on layer '{LayerMask.LayerToName(hit.collider.gameObject.layer)}'.");

            // --- The Safe Placement Logic ---
            // 1. Instantiate at origin
            GameObject newTile = Instantiate(tilePrefab, Vector3.zero, Quaternion.identity);

            // 2. Parent immediately
            newTile.transform.SetParent(activeFactory.transform, false);

            // 3. Move to position
            newTile.transform.position = hit.transform.position;
            newTile.transform.rotation = hit.transform.rotation;

            Debug.Log($"DEBUG: Tile placed at {hit.transform.position} and parented to {activeFactory.name}. Disabling socket.");

            // 4. Disable the socket
            hit.transform.gameObject.SetActive(false);
        }
        else
        {
            Debug.Log("DEBUG: Raycast MISSED. No socket found.");
        }
    }
}