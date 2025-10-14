using UnityEngine;
using UnityEngine.InputSystem;
using AutoForge.Core;
using System.Collections.Generic;

namespace AutoForge.Player
{
    public class PlayerBuilder : MonoBehaviour
    {
        public static PlayerBuilder Instance { get; private set; }

        [Header("Required References")]
        [SerializeField] private Camera mainCamera;
        [SerializeField] private Material previewValidMaterial;
        [SerializeField] private Material previewInvalidMaterial;

        [Header("Placement Settings")]
        [SerializeField] private LayerMask groundLayer;
        [SerializeField] private LayerMask factoryFloorLayer;
        [SerializeField] private float maxBuildDistance = 100f;
        [SerializeField] private float tileSize = 2f;
        [SerializeField] private float socketSelectionRadius = 150f;
        [Tooltip("Layers that will block tile placement (e.g., Default, Player, Enemies). DO NOT include FactoryFloor.")]
        [SerializeField] private LayerMask obstructionLayers;

        private GameObject buildPreview;
        private BuildingData currentBuildingData;
        private Renderer[] previewRenderers;
        private bool isBuildMode = false;
        public bool IsInBuildMode => isBuildMode;

        private bool isPlacingFactoryTile = false;
        private bool canPlace = false;
        private Transform lastHitSocket;
        private Collider[] overlapCheckResults = new Collider[5]; // Pre-allocate for performance

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        public void OnAttack(InputValue value)
        {
            if (isBuildMode && value.isPressed && canPlace)
            {
                PlaceBuilding();
            }
        }

        public void OnCancelPlacement(InputValue value)
        {
            if (isBuildMode && value.isPressed)
            {
                CancelBuildMode();
            }
        }

        private void Update()
        {
            if (isBuildMode && buildPreview != null)
            {
                MoveAndAlignPreview();
            }
        }

        public void SelectBuildingToPlace(BuildingData data)
        {
            if (data == null) { CancelBuildMode(); return; }

            isBuildMode = true;
            currentBuildingData = data;
            if (buildPreview != null) Destroy(buildPreview);

            buildPreview = Instantiate(currentBuildingData.buildingPrefab);
            isPlacingFactoryTile = buildPreview.GetComponent<FactoryTile>() != null;

            previewRenderers = buildPreview.GetComponentsInChildren<Renderer>();

            foreach (Collider col in buildPreview.GetComponentsInChildren<Collider>())
            {
                col.isTrigger = true;
            }

            SetLayerRecursively(buildPreview, LayerMask.NameToLayer("Ignore Raycast"));

            if (buildPreview.TryGetComponent<Building>(out var buildingScript))
            {
                buildingScript.enabled = false;
            }
        }

        public void CancelBuildMode()
        {
            if (buildPreview != null) Destroy(buildPreview);
            isBuildMode = false;
            currentBuildingData = null;
            isPlacingFactoryTile = false;
            lastHitSocket = null;
        }

        private void PlaceBuilding()
        {
            if (!canPlace) return;
            if (ResourceManager.Instance.HasResource(currentBuildingData.costType, currentBuildingData.costAmount))
            {
                ResourceManager.Instance.SpendResource(currentBuildingData.costType, currentBuildingData.costAmount);
                if (isPlacingFactoryTile)
                {
                    if (lastHitSocket == null || FactoryManager.Instance?.MobileFactory == null) return;
                    GameObject newTile = Instantiate(currentBuildingData.buildingPrefab, Vector3.zero, Quaternion.identity);
                    newTile.transform.SetParent(FactoryManager.Instance.MobileFactory.transform, false);
                    newTile.transform.position = buildPreview.transform.position;
                    newTile.transform.rotation = buildPreview.transform.rotation;
                    lastHitSocket.gameObject.SetActive(false);
                    FactoryManager.Instance.MobileFactory.GetComponent<FactoryNavMeshUpdater>().UpdateNavMesh();
                }
                else
                {
                    Instantiate(currentBuildingData.buildingPrefab, buildPreview.transform.position, buildPreview.transform.rotation);
                }
            }
        }

        private void MoveAndAlignPreview()
        {
            if (mainCamera == null) return;

            canPlace = false;
            lastHitSocket = null;
            Ray centerRay = mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
            buildPreview.transform.position = centerRay.GetPoint(10f); // Default floating position
            buildPreview.transform.rotation = Quaternion.identity;

            if (isPlacingFactoryTile)
            {
                Transform bestSocket = FindBestSocket();

                if (bestSocket != null)
                {
                    Transform parentTile = bestSocket.parent;
                    if (parentTile != null)
                    {
                        // --- PERFECT ALIGNMENT LOGIC ---
                        Vector3 direction = (bestSocket.position - parentTile.position).normalized;
                        Vector3 targetPosition = parentTile.position + (direction * tileSize);

                        // Explicitly lock the height to the parent tile's height
                        targetPosition.y = parentTile.position.y;

                        buildPreview.transform.position = targetPosition;
                        buildPreview.transform.rotation = parentTile.rotation;
                        // --- END ALIGNMENT LOGIC ---

                        // --- REFINED OBSTRUCTION CHECK ---
                        // Use a box slightly smaller than the tile to avoid hitting the adjacent parent tile.
                        Vector3 boxHalfExtents = new Vector3(tileSize / 2f, 0.1f, tileSize / 2f) * 0.95f;

                        // Use OverlapBoxNonAlloc which is more performant and tells us what we hit.
                        int overlapCount = Physics.OverlapBoxNonAlloc(targetPosition, boxHalfExtents, overlapCheckResults, parentTile.rotation, obstructionLayers, QueryTriggerInteraction.Ignore);

                        if (overlapCount == 0)
                        {
                            // If we didn't hit anything on an obstruction layer, placement is valid.
                            canPlace = true;
                            lastHitSocket = bestSocket;
                        }
                        else
                        {
                            // Optional: Log what we hit for debugging.
                            // Debug.LogWarning($"Placement blocked by: {overlapCheckResults[0].name}");
                        }
                        // --- END OBSTRUCTION CHECK ---
                    }
                }
            }
            else // Placing normal buildings
            {
                LayerMask combinedMask = groundLayer | factoryFloorLayer;
                if (Physics.Raycast(centerRay, out RaycastHit buildHit, maxBuildDistance, combinedMask))
                {
                    canPlace = true;
                    buildPreview.transform.position = buildHit.point;
                    buildPreview.transform.rotation = Quaternion.FromToRotation(Vector3.up, buildHit.normal);
                }
            }

            SetPreviewMaterial();
        }

        private Transform FindBestSocket()
        {
            Transform bestSocket = null;
            float closestDistSqr = socketSelectionRadius * socketSelectionRadius;
            Vector2 screenCenter = new Vector2(Screen.width / 2, Screen.height / 2);

            if (FactoryManager.Instance?.MobileFactory != null)
            {
                var allSockets = FactoryManager.Instance.MobileFactory.GetComponentsInChildren<Socket>();
                foreach (var socket in allSockets)
                {
                    if (!socket.gameObject.activeInHierarchy) continue;
                    Vector3 screenPoint = mainCamera.WorldToScreenPoint(socket.transform.position);
                    if (screenPoint.z > 0)
                    {
                        float distSqr = (new Vector2(screenPoint.x, screenPoint.y) - screenCenter).sqrMagnitude;
                        if (distSqr < closestDistSqr)
                        {
                            closestDistSqr = distSqr;
                            bestSocket = socket.transform;
                        }
                    }
                }
            }
            return bestSocket;
        }

        private void SetPreviewMaterial()
        {
            Material materialToApply = canPlace ? previewValidMaterial : previewInvalidMaterial;
            if (materialToApply != null && previewRenderers != null)
            {
                foreach (var renderer in previewRenderers)
                {
                    renderer.material = materialToApply;
                }
            }
        }

        private void SetLayerRecursively(GameObject obj, int layer)
        {
            obj.layer = layer;
            foreach (Transform child in obj.transform)
            {
                SetLayerRecursively(child.gameObject, layer);
            }
        }
    }
}