using UnityEngine;
using UnityEngine.InputSystem;
using AutoForge.Core;
using System.Collections.Generic;
using System.Text; // Required for the new debug logs

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
        [SerializeField] private LayerMask obstructionLayers;

        [Header("Tuning & Debugging")]
        [Tooltip("When true, enables obstruction and support checks. Turn this off to make placement less strict.")]
        public bool placementRulesEnabled = true;
        [Tooltip("How directly you must look at a socket to select it (lower is more forgiving).")]
        [SerializeField][Range(0.7f, 0.99f)] private float socketSelectionAngle = 0.9f;
        [Tooltip("Enable to print a single log message explaining why placement is failing.")]
        public bool enableDebugLogging = false;

        private GameObject buildPreview;
        private BuildingData currentBuildingData;
        private Renderer[] previewRenderers;
        private bool isBuildMode = false;
        public bool IsInBuildMode => isBuildMode;

        private bool isPlacingFactoryTile = false;
        private bool canPlace = false;
        private Transform lastHitSocket;
        private Collider[] overlapCheckResults = new Collider[10];
        private float currentPreviewRotationY = 0f;
        private StringBuilder debugStringBuilder = new StringBuilder(); // For efficient logging

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
            canPlace = false; // Assume we can't place until proven otherwise
            lastHitSocket = null;
            debugStringBuilder.Clear(); // Clear the debug log for this frame

            Ray centerRay = mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
            buildPreview.transform.position = centerRay.GetPoint(10f); // Default float position
            buildPreview.transform.rotation = Quaternion.Euler(0, currentPreviewRotationY, 0);

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

                        if (placementRulesEnabled)
                        {
                            if (!IsObstructed(targetPosition, parentPart.rotation, parentPart))
                            {
                                canPlace = true;
                                lastHitSocket = bestSocket;
                            }
                        }
                        else // Rules are off, just allow it
                        {
                            canPlace = true;
                            lastHitSocket = bestSocket;
                        }
                    }
                }
                else if (enableDebugLogging)
                {
                    debugStringBuilder.Append("[FAIL] No valid socket found in view cone. ");
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
                        // --- NEW ROBUST "ON-TOP" PLACEMENT ---
                        Transform hitTile = hit.collider.transform;
                        targetPosition = hitTile.position; // Snap X and Z to the center

                        // Use the collider's highest point for perfect height placement
                        targetPosition.y = hit.collider.bounds.max.y;

                        targetRotation = hitTile.rotation * Quaternion.Euler(0, currentPreviewRotationY, 0);
                    }
                    else
                    {
                        targetPosition = hit.point;
                        targetRotation = Quaternion.FromToRotation(Vector3.up, hit.normal) * Quaternion.Euler(0, currentPreviewRotationY, 0);
                    }

                    buildPreview.transform.position = targetPosition;
                    buildPreview.transform.rotation = targetRotation;

                    if (placementRulesEnabled)
                    {
                        if (!IsObstructed(targetPosition, targetRotation, null))
                        {
                            canPlace = true;
                        }
                    }
                    else
                    {
                        canPlace = true;
                    }
                }
                else if (enableDebugLogging)
                {
                    debugStringBuilder.Append("[FAIL] No valid build surface found. ");
                }
            }

            // Log the reason for failure if debug is on and we can't place
            if (enableDebugLogging && !canPlace && debugStringBuilder.Length > 0)
            {
                Debug.Log(debugStringBuilder.ToString());
            }

            SetPreviewMaterial();
        }

        private bool IsObstructed(Vector3 targetPosition, Quaternion targetRotation, Transform ignorePart)
        {
            Collider previewCollider = buildPreview.GetComponentInChildren<Collider>();
            if (previewCollider == null) return true; // Cannot check if there's no collider
            Vector3 boxHalfExtents = previewCollider.bounds.extents;

            int overlapCount = Physics.OverlapBoxNonAlloc(targetPosition, boxHalfExtents, overlapCheckResults, targetRotation, obstructionLayers, QueryTriggerInteraction.Ignore);
            if (overlapCount > 0)
            {
                if (enableDebugLogging) debugStringBuilder.Append($"[FAIL] Obstructed by '{overlapCheckResults[0].name}' on layer '{LayerMask.LayerToName(overlapCheckResults[0].gameObject.layer)}'. ");
                return true;
            }

            overlapCount = Physics.OverlapBoxNonAlloc(targetPosition, boxHalfExtents, overlapCheckResults, targetRotation, factoryFloorLayer, QueryTriggerInteraction.Ignore);
            if (overlapCount > 0)
            {
                for (int i = 0; i < overlapCount; i++)
                {
                    if (overlapCheckResults[i].transform != ignorePart)
                    {
                        if (enableDebugLogging) debugStringBuilder.Append($"[FAIL] Obstructed by other factory part: '{overlapCheckResults[i].name}'. ");
                        return true;
                    }
                }
            }
            return false;
        }

        // --- THIS IS THE NEW, INTUITIVE SOCKET FINDING METHOD ---
        private Transform FindBestSocket()
        {
            Transform bestSocket = null;
            float bestScore = -1f; // Use -1 to indicate no valid socket found yet

            if (FactoryManager.Instance?.MobileFactory == null) return null;

            Vector3 playerPos = transform.position;
            Vector3 playerLookDir = mainCamera.transform.forward;

            var allSockets = FactoryManager.Instance.MobileFactory.GetComponentsInChildren<Socket>();

            foreach (var socket in allSockets)
            {
                if (!socket.gameObject.activeInHierarchy) continue;

                Vector3 toSocketDir = (socket.transform.position - playerPos).normalized;
                float dot = Vector3.Dot(playerLookDir, toSocketDir);

                float distSqr = (socket.transform.position - playerPos).sqrMagnitude;
                if (distSqr > maxBuildDistance * maxBuildDistance) continue; // Skip if too far

                if (dot > socketSelectionAngle)
                {
                    // A simple score: how aligned it is, divided by distance.
                    // This naturally prioritizes close objects you are looking directly at.
                    float score = dot / distSqr;

                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestSocket = socket.transform;
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