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
        [SerializeField] private GameObject debugSpherePrefab;

        [Header("Placement Settings")]
        [SerializeField] private LayerMask groundLayer;
        [SerializeField] private LayerMask factoryFloorLayer;
        [SerializeField] private float maxBuildDistance = 100f;
        [SerializeField] private float tileSize = 2f;
        [SerializeField] private float socketSelectionRadius = 150f;
        [SerializeField] private LayerMask obstructionLayers;

        private GameObject buildPreview;
        private BuildingData currentBuildingData;
        private Renderer[] previewRenderers;
        private GameObject debugSphereInstance;
        private bool isBuildMode = false;
        public bool IsInBuildMode => isBuildMode;

        private bool isPlacingFactoryTile = false;
        private bool canPlace = false;
        private Transform lastHitSocket;
        private Collider[] overlapCheckResults = new Collider[10];
        private float currentPreviewRotationY = 0f;

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

        public void OnRotate(InputValue value)
        {
            if (!isBuildMode || isPlacingFactoryTile) return;
            float scrollValue = value.Get<Vector2>().y;
            if (scrollValue > 0) currentPreviewRotationY += 90f;
            else if (scrollValue < 0) currentPreviewRotationY -= 90f;
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

            currentPreviewRotationY = 0f;
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

            if (debugSpherePrefab != null && debugSphereInstance == null)
            {
                debugSphereInstance = Instantiate(debugSpherePrefab);
                // CRITICAL FIX: Make the debug sphere ignore raycasts!
                SetLayerRecursively(debugSphereInstance, LayerMask.NameToLayer("Ignore Raycast"));
            }
        }

        public void CancelBuildMode()
        {
            if (buildPreview != null) Destroy(buildPreview);
            if (debugSphereInstance != null) Destroy(debugSphereInstance);
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
                    GameObject newBuilding = Instantiate(currentBuildingData.buildingPrefab, buildPreview.transform.position, buildPreview.transform.rotation);
                    Ray ray = mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
                    if (Physics.Raycast(ray, out RaycastHit hit, maxBuildDistance, factoryFloorLayer))
                    {
                        newBuilding.transform.SetParent(FactoryManager.Instance.MobileFactory.transform, true);
                    }
                }
            }
        }

        private void MoveAndAlignPreview()
        {
            canPlace = false;
            lastHitSocket = null;
            Ray centerRay = mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
            buildPreview.transform.position = centerRay.GetPoint(10f);
            buildPreview.transform.rotation = Quaternion.Euler(0, currentPreviewRotationY, 0);

            // --- VISUAL DEBUGGER ---
            if (debugSphereInstance != null)
            {
                if (Physics.Raycast(centerRay, out RaycastHit generalHit, maxBuildDistance))
                {
                    debugSphereInstance.SetActive(true);
                    debugSphereInstance.transform.position = generalHit.point;
                }
                else
                {
                    debugSphereInstance.SetActive(false);
                }
            }

            if (isPlacingFactoryTile)
            {
                Transform bestSocket = FindBestSocket();
                if (bestSocket != null)
                {
                    Transform parentPart = bestSocket.parent;
                    if (parentPart != null)
                    {
                        Vector3 direction = (bestSocket.position - parentPart.position).normalized;
                        Vector3 targetPosition = parentPart.position + (direction * tileSize);
                        targetPosition.y = parentPart.position.y;
                        buildPreview.transform.position = targetPosition;
                        buildPreview.transform.rotation = parentPart.rotation;

                        if (!IsObstructed(targetPosition, parentPart.rotation, parentPart))
                        {
                            canPlace = true;
                            lastHitSocket = bestSocket;
                        }
                    }
                }
            }
            else // Placing normal buildings
            {
                LayerMask combinedMask = groundLayer | factoryFloorLayer;
                if (Physics.Raycast(centerRay, out RaycastHit hit, maxBuildDistance, combinedMask))
                {
                    Vector3 targetPosition;
                    Quaternion targetRotation;

                    if (((1 << hit.collider.gameObject.layer) & factoryFloorLayer) != 0)
                    {
                        Transform hitTile = hit.collider.transform;
                        targetPosition = hitTile.position;
                        float tileTopY = hit.collider.bounds.max.y;
                        float buildingHalfHeight = buildPreview.GetComponentInChildren<Renderer>().bounds.extents.y;
                        targetPosition.y = tileTopY + buildingHalfHeight;
                        targetRotation = hitTile.rotation * Quaternion.Euler(0, currentPreviewRotationY, 0);
                    }
                    else
                    {
                        targetPosition = hit.point;
                        targetRotation = Quaternion.FromToRotation(Vector3.up, hit.normal) * Quaternion.Euler(0, currentPreviewRotationY, 0);
                    }

                    buildPreview.transform.position = targetPosition;
                    buildPreview.transform.rotation = targetRotation;

                    if (!IsObstructed(targetPosition, targetRotation, null))
                    {
                        canPlace = true;
                    }
                }
            }
            SetPreviewMaterial();
        }

        // --- THIS IS THE UPDATED OBSTRUCTION METHOD WITH DEBUGGING ---
        private bool IsObstructed(Vector3 targetPosition, Quaternion targetRotation, Transform ignorePart)
        {
            Bounds previewBounds = buildPreview.GetComponentInChildren<Renderer>().bounds;
            Vector3 boxHalfExtents = previewBounds.extents;

            // Check for environmental/major obstructions
            int overlapCount = Physics.OverlapBoxNonAlloc(targetPosition, boxHalfExtents, overlapCheckResults, targetRotation, obstructionLayers, QueryTriggerInteraction.Ignore);
            if (overlapCount > 0)
            {
                Debug.LogWarning($"[OBSTRUCTION] Blocked by environmental object: '{overlapCheckResults[0].name}' on layer '{LayerMask.LayerToName(overlapCheckResults[0].gameObject.layer)}'");
                return true;
            }

            // Check for other factory tiles
            overlapCount = Physics.OverlapBoxNonAlloc(targetPosition, boxHalfExtents, overlapCheckResults, targetRotation, factoryFloorLayer, QueryTriggerInteraction.Ignore);
            if (overlapCount > 0)
            {
                for (int i = 0; i < overlapCount; i++)
                {
                    // If the object we hit is NOT the part we are building from, it's a true obstruction.
                    if (overlapCheckResults[i].transform != ignorePart)
                    {
                        Debug.LogWarning($"[OBSTRUCTION] Blocked by other factory part: '{overlapCheckResults[i].name}'. We were ignoring '{ignorePart?.name}'.");
                        return true;
                    }
                }
            }

            return false;
        }

        private Transform FindBestSocket()
        {
            // ... (This method remains the same and is working)
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