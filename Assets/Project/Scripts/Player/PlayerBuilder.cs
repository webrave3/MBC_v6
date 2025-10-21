using UnityEngine;
using UnityEngine.InputSystem;
using AutoForge.Core;
using System.Collections.Generic;
using System.Text;

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
        [SerializeField] private LayerMask factoryFloorLayer;
        [Tooltip("The layers that buildings can be placed on. IMPORTANT: This should include your FactoryFloorLayer AND the layer your placed buildings are on.")]
        [SerializeField] private LayerMask buildingPlacementLayers;
        [SerializeField] private float maxBuildDistance = 100f;
        [SerializeField] private float tileSize = 2f;
        [SerializeField] private LayerMask obstructionLayers;

        [Header("Tuning & Debugging")]
        [Tooltip("When true, enables obstruction checks. When false, stacking and surface checks still apply.")]
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
        private StringBuilder debugStringBuilder = new StringBuilder();

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
            if (!canPlace || currentBuildingData == null) return;
            if (ResourceManager.Instance.HasResource(currentBuildingData.costType, currentBuildingData.costAmount))
            {
                ResourceManager.Instance.SpendResource(currentBuildingData.costType, currentBuildingData.costAmount);
                if (isPlacingFactoryTile)
                {
                    if (lastHitSocket == null || FactoryManager.Instance?.PlayerFactory == null) return;
                    GameObject newTile = Instantiate(currentBuildingData.buildingPrefab, buildPreview.transform.position, buildPreview.transform.rotation);
                    newTile.transform.SetParent(FactoryManager.Instance.PlayerFactory.transform, true);
                    lastHitSocket.gameObject.SetActive(false);
                    FactoryManager.Instance.PlayerFactory.GetComponent<FactoryNavMeshUpdater>().UpdateNavMesh();
                }
                else
                {
                    GameObject newBuilding = Instantiate(currentBuildingData.buildingPrefab, buildPreview.transform.position, buildPreview.transform.rotation);
                    if (FactoryManager.Instance?.PlayerFactory != null)
                    {
                        newBuilding.transform.SetParent(FactoryManager.Instance.PlayerFactory.transform, true);
                    }
                }
            }
        }

        private void MoveAndAlignPreview()
        {
            canPlace = false;
            lastHitSocket = null;
            debugStringBuilder.Clear();

            Ray centerRay = mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
            buildPreview.transform.position = centerRay.GetPoint(10f);
            buildPreview.transform.rotation = Quaternion.Euler(0, currentPreviewRotationY, 0);

            if (isPlacingFactoryTile)
            {
                HandleFactoryTilePlacement(centerRay);
            }
            else
            {
                HandleBuildingPlacement(centerRay);
            }

            if (enableDebugLogging && !canPlace && debugStringBuilder.Length > 0)
            {
                Debug.Log(debugStringBuilder.ToString());
            }

            SetPreviewMaterial();
        }

        private void HandleFactoryTilePlacement(Ray centerRay)
        {
            Transform bestSocket = FindBestSocket();
            if (bestSocket != null)
            {
                Transform parentPart = bestSocket.parent;
                if (parentPart != null)
                {
                    Vector3 direction = (bestSocket.position - parentPart.position).normalized;
                    Vector3 targetPosition = parentPart.position + (direction * tileSize);

                    if (FactoryManager.Instance?.PlayerFactory != null)
                    {
                        targetPosition.y = FactoryManager.Instance.PlayerFactory.transform.position.y;
                    }

                    buildPreview.transform.position = targetPosition;
                    buildPreview.transform.rotation = parentPart.rotation;

                    canPlace = placementRulesEnabled ? !IsObstructed(targetPosition, parentPart.rotation, parentPart) : true;

                    if (canPlace)
                    {
                        lastHitSocket = bestSocket;
                    }
                }
            }
            else if (enableDebugLogging)
            {
                debugStringBuilder.Append("[FAIL] No valid socket found in view cone. ");
            }
        }

        // MODIFIED: This method is rewritten to correctly handle stacking when placement rules are disabled.
        private void HandleBuildingPlacement(Ray centerRay)
        {
            // Raycast against all valid building surfaces (floor AND other buildings).
            if (Physics.Raycast(centerRay, out RaycastHit hit, maxBuildDistance, buildingPlacementLayers))
            {
                canPlace = false; // Default to invalid.

                Building existingBuilding = hit.collider.GetComponentInParent<Building>();

                // CASE 1: STACKING. This is the highest priority check.
                if (existingBuilding != null && currentBuildingData.canStack && existingBuilding.data.buildingName == currentBuildingData.buildingName)
                {
                    Vector3 targetPosition = existingBuilding.transform.position + new Vector3(0, currentBuildingData.stackHeight, 0);
                    Quaternion targetRotation = existingBuilding.transform.rotation;
                    buildPreview.transform.position = targetPosition;
                    buildPreview.transform.rotation = targetRotation;

                    // Stacking is allowed, but we check for obstructions if rules are on.
                    canPlace = placementRulesEnabled ? !IsObstructed(targetPosition, targetRotation, existingBuilding.transform) : true;
                }
                // CASE 2: PLACING ON EMPTY TILE. Only runs if we are not stacking.
                else if (existingBuilding == null)
                {
                    Transform hitTile = hit.collider.transform;
                    Vector3 targetPosition = hitTile.position;
                    targetPosition.y = hit.collider.bounds.max.y + currentBuildingData.placementYOffset;
                    Quaternion targetRotation = hitTile.rotation * Quaternion.Euler(0, currentPreviewRotationY, 0);
                    buildPreview.transform.position = targetPosition;
                    buildPreview.transform.rotation = targetRotation;

                    // Standard placement is allowed, but check for obstructions if rules are on.
                    canPlace = placementRulesEnabled ? !IsObstructed(targetPosition, targetRotation, null) : true;
                }
                // CASE 3: INVALID PLACEMENT. We hit a building but couldn't stack.
                else
                {
                    buildPreview.transform.position = hit.point;
                    canPlace = false;
                    if (enableDebugLogging) debugStringBuilder.Append("[FAIL] Target is occupied by a non-stackable or different building.");
                }
            }
            else
            {
                canPlace = false;
                if (enableDebugLogging) debugStringBuilder.Append("[FAIL] No valid build surface found.");
            }
        }

        private bool IsObstructed(Vector3 targetPosition, Quaternion targetRotation, Transform ignorePart)
        {
            Collider previewCollider = buildPreview.GetComponentInChildren<Collider>();
            if (previewCollider == null) return true;
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
                    if (overlapCheckResults[i].transform != ignorePart && (ignorePart == null || !overlapCheckResults[i].transform.IsChildOf(ignorePart)))
                    {
                        if (enableDebugLogging) debugStringBuilder.Append($"[FAIL] Obstructed by other factory part: '{overlapCheckResults[i].name}'. ");
                        return true;
                    }
                }
            }
            return false;
        }

        private Transform FindBestSocket()
        {
            Transform bestSocket = null;
            float bestScore = -1f;

            if (FactoryManager.Instance?.PlayerFactory == null) return null;

            Vector3 playerPos = transform.position;
            Vector3 playerLookDir = mainCamera.transform.forward;

            var allSockets = FactoryManager.Instance.PlayerFactory.GetComponentsInChildren<Socket>();

            foreach (var socket in allSockets)
            {
                if (!socket.gameObject.activeInHierarchy) continue;

                Vector3 toSocketDir = (socket.transform.position - playerPos).normalized;
                float dot = Vector3.Dot(playerLookDir, toSocketDir);

                float distSqr = (socket.transform.position - playerPos).sqrMagnitude;
                if (distSqr > maxBuildDistance * maxBuildDistance) continue;

                if (dot > socketSelectionAngle)
                {
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