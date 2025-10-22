using UnityEngine;
using UnityEngine.InputSystem;
using AutoForge.Core;
using AutoForge.Factory;
using System.Collections.Generic;
using System.Text; // Keep for potential future use

namespace AutoForge.Player
{
    public class PlayerBuilder : MonoBehaviour
    {
        public static PlayerBuilder Instance { get; private set; }

        [Header("Required References")]
        [SerializeField] private Camera mainCamera;
        [SerializeField] private Material previewValidMaterial; // Fallback
        [SerializeField] private Material previewInvalidMaterial; // Fallback

        [Header("Placement Settings")]
        [Tooltip("Layer mask containing ONLY the Factory Tile objects.")]
        [SerializeField] private LayerMask factoryTileLayer = 1 << 6; // Example: Assign layer 6
        [Tooltip("Layer mask containing Factory Tiles AND any other surfaces buildings can sit on.")]
        [SerializeField] private LayerMask buildingPlacementLayers = 1 << 6 | 1 << 0; // Example: Tile + Default
        [SerializeField] private float maxBuildDistance = 10f;
        [SerializeField] private float tileSize = 2f;

        [Header("Tuning")]
        [SerializeField, Range(0.7f, 0.99f)] private float socketSelectionAngle = 0.9f;
        [SerializeField] private float buildingYOffset = 0.01f; // Small gap

        // --- Private State ---
        private GameObject buildPreview;
        private BuildingData currentBuildingData;
        private bool isBuildMode = false;
        private bool canPlace = false;
        private Transform lastHitSocket;
        private Transform lastHitSurface;
        private Vector3 lastValidPreviewPosition;
        private Quaternion lastValidPreviewRotation;
        private Collider previewCollider;

        // --- Material Handling ---
        private static readonly int ColorPropertyID = Shader.PropertyToID("_BaseColor");
        private List<Renderer> previewRenderers = new List<Renderer>();
        private Dictionary<Renderer, Material[]> originalMaterials = new Dictionary<Renderer, Material[]>();
        private List<Material> previewMaterialInstances = new List<Material>();

        public bool IsBuildMode => isBuildMode;

        // --- Input Action Callbacks ---
        public void OnAttack(InputValue value) { if (isBuildMode && value.isPressed && canPlace) PlaceItem(); }
        public void OnCancelPlacement(InputValue value) { if (isBuildMode && value.isPressed) CancelBuildMode(); }

        // --- Unity Methods ---
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            if (mainCamera == null) { mainCamera = Camera.main; if (mainCamera == null) Debug.LogError("[PlayerBuilder] Camera not assigned!", this); }
        }
        private void Update()
        { if (isBuildMode && buildPreview != null) UpdatePreviewPositionAndValidation(); }

        // --- Public API ---
        public void SelectBuildingToPlace(BuildingData data)
        {
            // (Creates preview, disables physics/scripts - unchanged)
            if (data == null) { CancelBuildMode(); return; }
            RevertPreviewMaterials(); if (buildPreview != null) Destroy(buildPreview);
            isBuildMode = true; currentBuildingData = data; buildPreview = Instantiate(currentBuildingData.buildingPrefab); if (buildPreview == null) { Debug.LogError($"Failed preview: {data.buildingName}", this); CancelBuildMode(); return; }
            previewRenderers.Clear(); previewRenderers.AddRange(buildPreview.GetComponentsInChildren<Renderer>()); previewCollider = buildPreview.GetComponentInChildren<Collider>();
            if (buildPreview.TryGetComponent<Rigidbody>(out var rb)) { rb.isKinematic = true; rb.detectCollisions = false; }
            if (previewCollider != null) { previewCollider.enabled = false; }
            foreach (var s in buildPreview.GetComponentsInChildren<MonoBehaviour>()) { if (!(s is FactoryTile)) s.enabled = false; }
            SetLayerRecursively(buildPreview, LayerMask.NameToLayer("Ignore Raycast")); previewMaterialInstances.Clear();
        }
        public void CancelBuildMode()
        {
            // (Reverts materials, resets state - unchanged)
            RevertPreviewMaterials(); if (buildPreview != null) Destroy(buildPreview); isBuildMode = false; currentBuildingData = null; lastHitSocket = null; lastHitSurface = null; canPlace = false; previewCollider = null;
        }

        // --- Core Placement Logic ---
        private void PlaceItem()
        {
            // (Resource checks unchanged)
            if (!canPlace || currentBuildingData == null || FactoryManager.Instance == null) return; if (!ResourceManager.Instance.HasResource(currentBuildingData.costType, currentBuildingData.costAmount)) return; ResourceManager.Instance.SpendResource(currentBuildingData.costType, currentBuildingData.costAmount);

            // Instantiate using the STORED valid transform
            GameObject newObject = Instantiate(currentBuildingData.buildingPrefab, lastValidPreviewPosition, lastValidPreviewRotation);
            FactoryTile newTileScript = newObject.GetComponent<FactoryTile>();

            if (newTileScript != null) // It's a Tile
            {
                int layerIndex = LayerMaskToLayer(factoryTileLayer.value);
                if (layerIndex != -1) SetLayerRecursively(newObject, layerIndex); else Debug.LogError("Could not get layer index!", this);
                HandleTilePlacement(newObject, newTileScript);
            }
            else // It's a Building
            { HandleBuildingPlacement(newObject); }
        }

        private void HandleTilePlacement(GameObject newTileObject, FactoryTile newTileScript)
        {
            MobileFactory playerFactory = FactoryManager.Instance.PlayerFactory;
            if (lastHitSocket == null || playerFactory == null) { /* Error Handling */ Destroy(newTileObject); ResourceManager.Instance.AddResource(currentBuildingData.costType, currentBuildingData.costAmount); return; }
            Rigidbody anchorRigidbody = lastHitSocket.GetComponentInParent<Rigidbody>();
            if (anchorRigidbody == null) { /* Error Handling */ Destroy(newTileObject); ResourceManager.Instance.AddResource(currentBuildingData.costType, currentBuildingData.costAmount); return; }

            // --- DEBUG: Log positions BEFORE joint/parenting ---
            Vector3 newTileInitialPos = newTileObject.transform.position;
            // ---> UNCOMMENT TO DEBUG TELEPORT <---
            // Debug.Log($"[DEBUG] HandleTilePlacement: New tile initial world pos: {newTileInitialPos:F3}");

            // --- Configure Joint (Strong settings - unchanged) ---
            ConfigurableJoint joint = newTileObject.AddComponent<ConfigurableJoint>();
            joint.connectedBody = anchorRigidbody; joint.xMotion = ConfigurableJointMotion.Locked; joint.yMotion = ConfigurableJointMotion.Locked; joint.zMotion = ConfigurableJointMotion.Locked;
            joint.angularXMotion = ConfigurableJointMotion.Limited; joint.angularYMotion = ConfigurableJointMotion.Limited; joint.angularZMotion = ConfigurableJointMotion.Limited;
            float angularLimitValue = 1f; SoftJointLimit linearLimit = new SoftJointLimit { limit = 0.005f }; joint.linearLimit = linearLimit;
            SoftJointLimit angularLimit = new SoftJointLimit { limit = angularLimitValue, bounciness = 0f, contactDistance = 0f };
            joint.lowAngularXLimit = new SoftJointLimit { limit = -angularLimitValue, bounciness = 0f, contactDistance = 0f };
            joint.highAngularXLimit = angularLimit; joint.angularYLimit = angularLimit; joint.angularZLimit = angularLimit;
            var slerpDrive = new JointDrive { positionSpring = 15000f, positionDamper = 750f, maximumForce = float.MaxValue }; joint.slerpDrive = slerpDrive;
            joint.projectionMode = JointProjectionMode.PositionAndRotation; joint.projectionDistance = 0.005f; joint.projectionAngle = 0.5f;
            joint.configuredInWorldSpace = false;
            // --- End Joint ---

            newTileObject.transform.SetParent(playerFactory.transform, true); // Parent
            playerFactory.RegisterTile(newTileScript); // Register
            lastHitSocket.gameObject.SetActive(false); // Disable used socket

            // --- DEBUG: Check if position changed drastically ---
            Vector3 newTileFinalPos = newTileObject.transform.position;
            float posDelta = Vector3.Distance(newTileInitialPos, newTileFinalPos);
            if (posDelta > 0.1f)
            { // Check if moved more than 10cm
              // ---> UNCOMMENT TO DEBUG TELEPORT <---
              // Debug.LogWarning($"[DEBUG] HandleTilePlacement: Tile '{newTileObject.name}' POS SHIFT! Delta={posDelta:F3}. Initial={newTileInitialPos:F3}, Final={newTileFinalPos:F3}");
            }
            // --- END DEBUG ---

            // Handle new sockets (unchanged)
            foreach (Socket s in newTileObject.GetComponentsInChildren<Socket>(true)) { Vector3 d = (anchorRigidbody.transform.position - newTileObject.transform.position).normalized; Vector3 f = s.transform.forward; s.gameObject.SetActive(Vector3.Dot(d, f) < 0.95f); }
            playerFactory.GetComponent<FactoryNavMeshUpdater>()?.UpdateNavMesh(); // Update obstacle
        }

        private void HandleBuildingPlacement(GameObject newBuilding)
        {
            // (Parenting and enabling scripts - unchanged)
            if (lastHitSurface == null) { /* Error */ Destroy(newBuilding); ResourceManager.Instance.AddResource(currentBuildingData.costType, currentBuildingData.costAmount); return; }
            newBuilding.transform.SetParent(lastHitSurface, true);
            foreach (var s in newBuilding.GetComponentsInChildren<MonoBehaviour>(true)) { s.enabled = true; }
        }

        // --- Preview Update Logic ---
        private void UpdatePreviewPositionAndValidation()
        {
            // (Raycasting and basic logic - unchanged)
            canPlace = false; lastHitSocket = null; lastHitSurface = null; if (mainCamera == null || buildPreview == null) return;
            Ray centerRay = mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0)); bool isTile = buildPreview.GetComponent<FactoryTile>() != null;
            Vector3 currentPos = buildPreview.transform.position; Quaternion currentRot = buildPreview.transform.rotation;
            if (isTile)
            {
                Transform socket = FindBestSocket(centerRay);
                if (socket != null)
                {
                    lastHitSocket = socket; Transform parent = socket.GetComponentInParent<Rigidbody>()?.transform;
                    if (parent != null) { Vector3 dir = (socket.position - parent.position).normalized; currentPos = parent.position + (dir * tileSize); currentRot = parent.rotation; canPlace = true; Debug.DrawLine(centerRay.origin, currentPos, Color.green, 0f, false); }
                    else { PlacePreviewAtRayPoint(centerRay); }
                }
                else { PlacePreviewAtRayPoint(centerRay); Debug.DrawLine(centerRay.origin, buildPreview.transform.position, Color.red, 0f, false); }
            }
            else
            {
                // Try placing building on surface
                if (PlacePreviewOnSurface(centerRay, out currentPos, out currentRot))
                {
                    canPlace = true;
                    // DEBUG: Draw cyan line from camera to valid building preview position
                    Debug.DrawLine(centerRay.origin, currentPos, Color.cyan, 0f, false);
                }
                else { PlacePreviewAtRayPoint(centerRay); } // Raycast failed
            }
            buildPreview.transform.position = currentPos; buildPreview.transform.rotation = currentRot;
            if (canPlace) { lastValidPreviewPosition = currentPos; lastValidPreviewRotation = currentRot; } // Store valid transform
            SetPreviewMaterial();
        }

        /// <summary> Calculates building preview position/rotation ON TOP of surfaces, snapping to tile centers. </summary>
        private bool PlacePreviewOnSurface(Ray ray, out Vector3 targetPosition, out Quaternion targetRotation)
        {
            targetPosition = Vector3.zero; targetRotation = Quaternion.identity;

            if (Physics.Raycast(ray, out RaycastHit hit, maxBuildDistance, buildingPlacementLayers))
            {
                lastHitSurface = hit.transform; // Store original hit transform
                FactoryTile hitTile = hit.collider.GetComponentInParent<FactoryTile>();

                Vector3 targetPositionBase; // XZ position on the surface
                Vector3 surfaceUp = hit.normal;
                float surfaceTopY = hit.point.y; // Y position on the surface

                // --- Determine Base Position, Rotation, and Top Y ---
                if (hitTile != null) // --- Snapping to Tile ---
                {
                    lastHitSurface = hitTile.transform; // Target TILE transform
                    surfaceUp = hitTile.transform.up; // Use tile's up
                    targetRotation = hitTile.transform.rotation; // Use tile's rotation
                    targetPositionBase = hitTile.transform.position; // Tile's center for XZ

                    // *** CORRECTED V4: Calculate tile's top Y using Collider bounds center + extents TRANSFORMED by rotation ***
                    Collider tileCollider = hitTile.GetComponent<Collider>();
                    if (tileCollider != null)
                    {
                        Vector3 centerWorld = tileCollider.bounds.center;
                        Vector3 extents = tileCollider.bounds.extents;
                        // Transform the local up extents vector into world space based on the tile's rotation
                        Vector3 worldUpExtents = hitTile.transform.TransformDirection(Vector3.up * extents.y);
                        // Top Y is the center Y + the magnitude of the world-space up extents vector
                        surfaceTopY = centerWorld.y + worldUpExtents.magnitude;

                        // --- DEBUG: Visualize calculated top point slightly above surface ---
                        // Create a point at the tile's center XZ but at the calculated top Y
                        Vector3 topPointViz = targetPositionBase;
                        topPointViz.y = surfaceTopY;
                        // Draw a short magenta ray pointing UP from this calculated point
                        Debug.DrawRay(topPointViz, surfaceUp * 0.5f, Color.magenta);
                        // --- END DEBUG ---

                    }
                    else { surfaceTopY = hitTile.transform.position.y + (tileSize / 2f); } // Estimate height

                    // Set base Y coordinate ONLY, keep snapped XZ from tile center
                    targetPositionBase.y = surfaceTopY;

                }
                else // --- Placing on Other Surface ---
                {
                    targetPositionBase = hit.point; // Use exact hit point XZ and Y
                    surfaceTopY = hit.point.y;      // Top Y is the hit point Y
                                                    // Align rotation to surface normal
                    Vector3 projectedForward = Vector3.ProjectOnPlane(mainCamera.transform.forward, surfaceUp).normalized;
                    if (projectedForward.sqrMagnitude < 0.01f) projectedForward = Vector3.ProjectOnPlane(mainCamera.transform.up, surfaceUp).normalized;
                    if (projectedForward.sqrMagnitude > 0.01f) targetRotation = Quaternion.LookRotation(projectedForward, surfaceUp);
                    else targetRotation = Quaternion.LookRotation(Vector3.forward, surfaceUp);
                }

                // --- Calculate Building's Base Offset ---
                float bottomYOffset = 0f;
                if (previewCollider != null)
                {
                    Quaternion initialRot = buildPreview.transform.rotation;
                    buildPreview.transform.rotation = targetRotation; // Align rotation FIRST

                    // Calculate bounds WHILE aligned
                    Bounds alignedBounds = previewCollider.bounds;
                    // Find the lowest point in WORLD space
                    Vector3 worldBoundsMin = alignedBounds.min;
                    // Project the pivot's world position onto the surface normal
                    float pivotProj = Vector3.Dot(buildPreview.transform.position, surfaceUp.normalized);
                    // Project the world bounds min point onto the surface normal
                    float minProj = Vector3.Dot(worldBoundsMin, surfaceUp.normalized);
                    // Offset is distance from pivot projection down to min projection
                    bottomYOffset = pivotProj - minProj;

                    buildPreview.transform.rotation = initialRot; // Restore rotation for smooth preview

                    // DEBUG: Log offset calculation
                    // Debug.Log($"PlacePreview: Building Offset Calc: WorldMinY={worldBoundsMin.y:F3}, PivotY={buildPreview.transform.position.y:F3}, SurfaceUp={surfaceUp:F2}, PivotProj={pivotProj:F3}, MinProj={minProj:F3}, Offset={bottomYOffset:F3}");
                }

                // --- Set Final Target Position ---
                // Start at the base position (Tile center XZ, Tile top Y OR hit point)
                // Add the surface's UP vector scaled by (building's base offset + tiny gap)
                targetPosition = targetPositionBase + surfaceUp * (bottomYOffset + buildingYOffset);

                // --- DEBUG: Visualize Final Target Position ---
                Debug.DrawLine(targetPositionBase, targetPosition, Color.yellow); // Line from surface to final pos
                                                                                  // --- END DEBUG ---

                return true; // Raycast hit, calculation successful
            }
            else { return false; } // Raycast failed
        }


        private void PlacePreviewAtRayPoint(Ray ray)
        { if (buildPreview != null) { buildPreview.transform.position = ray.GetPoint(maxBuildDistance * 0.75f); } canPlace = false; }

        private Transform FindBestSocket(Ray viewRay)
        {
            // (Unchanged - includes debug logs if uncommented)
            Transform bestSocket = null; float bestScore = -1f;
            if (Physics.Raycast(viewRay, out RaycastHit hit, maxBuildDistance, factoryTileLayer))
            {
                // Debug.Log($"FindBestSocket Raycast HIT: {hit.collider.name} on Layer: {LayerMask.LayerToName(hit.collider.gameObject.layer)}"); // DEBUG
                FactoryTile hitTile = hit.collider.GetComponentInParent<FactoryTile>();
                if (hitTile != null)
                {
                    // Debug.Log($"FindBestSocket: Hit FactoryTile: {hitTile.name}. Searching sockets..."); // DEBUG
                    Vector3 lookDir = mainCamera.transform.forward; int activeSocketCount = 0;
                    foreach (var socket in hitTile.GetComponentsInChildren<Socket>())
                    {
                        if (!socket.gameObject.activeInHierarchy) continue; activeSocketCount++;
                        Vector3 toSocketDir = (socket.transform.position - viewRay.origin).normalized;
                        float dot = Vector3.Dot(lookDir, toSocketDir);
                        if (dot > socketSelectionAngle)
                        {
                            float distSqr = (socket.transform.position - viewRay.origin).sqrMagnitude;
                            float score = dot / (1f + distSqr);
                            if (score > bestScore) { bestScore = score; bestSocket = socket.transform; }
                        }
                    }
                    // if(bestSocket == null && activeSocketCount > 0) Debug.Log($"FindBestSocket: Hit tile {hitTile.name}, checked {activeSocketCount} active sockets but none aligned (Dot < {socketSelectionAngle})."); // DEBUG
                }
                else { Debug.LogWarning($"FindBestSocket: Raycast hit {hit.collider.name} but it has no FactoryTile component in parents."); }
            }
            else { /* Optional: Log raycast miss */ }
            return bestSocket;
        }

        // --- Material Handling Methods ---
        private void SetPreviewMaterial() { /* ... unchanged ... */ if (previewRenderers.Count == 0 && buildPreview != null) previewRenderers.AddRange(buildPreview.GetComponentsInChildren<Renderer>()); if (previewRenderers.Count == 0) return; if (previewMaterialInstances.Count == 0) { originalMaterials.Clear(); foreach (var r in previewRenderers) { if (r != null) { originalMaterials[r] = r.sharedMaterials; previewMaterialInstances.AddRange(r.materials); } } } Color tC = canPlace ? new Color(0.5f, 1.0f, 0.5f, 0.75f) : new Color(1.0f, 0.5f, 0.5f, 0.75f); bool uTC = false; foreach (Material mI in previewMaterialInstances) { if (mI != null && mI.HasProperty(ColorPropertyID)) { mI.SetColor(ColorPropertyID, tC); uTC = true; } } if (!uTC) { Material fM = canPlace ? previewValidMaterial : previewInvalidMaterial; if (fM == null) return; foreach (var r in previewRenderers) { if (r != null) { var ms = new Material[r.sharedMaterials.Length]; for (int i = 0; i < ms.Length; i++) ms[i] = fM; r.materials = ms; } } } }
        private void RevertPreviewMaterials() { /* ... unchanged ... */ foreach (var kvp in originalMaterials) { if (kvp.Key != null) kvp.Key.sharedMaterials = kvp.Value; } originalMaterials.Clear(); foreach (var i in previewMaterialInstances) { if (i != null) Destroy(i); } previewMaterialInstances.Clear(); previewRenderers.Clear(); }

        // --- Utility Methods ---
        private void SetLayerRecursively(GameObject obj, int layer) { /* ... unchanged ... */ if (obj == null) return; obj.layer = layer; foreach (Transform c in obj.transform) { if (c != null) SetLayerRecursively(c.gameObject, layer); } }
        private int LayerMaskToLayer(int layerMaskValue) { /* ... unchanged ... */ if (layerMaskValue == 0) return -1; int l = 0; int m = layerMaskValue; while ((m & 1) == 0) { m >>= 1; l++; if (l > 31) return -1; } return l; }

    } // End of Class
} // End of Namespace