using UnityEngine;
using UnityEngine.InputSystem;
using AutoForge.Core;
using AutoForge.Factory;
using System.Collections.Generic;

namespace AutoForge.Player
{
    public class PlayerBuilder : MonoBehaviour
    {
        public static PlayerBuilder Instance { get; private set; }

        [Header("Required References")]
        [SerializeField] private Camera mainCamera;
        // NOTE: These are now FALLBACKS if the tinting shader property isn't found.
        [SerializeField] private Material previewValidMaterial;
        [SerializeField] private Material previewInvalidMaterial;

        [Header("Placement Settings")]
        [Tooltip("Layer mask containing ONLY the Factory Tile objects.")]
        [SerializeField] private LayerMask factoryTileLayer;
        [Tooltip("Layer mask containing Factory Tiles AND any other surfaces buildings can sit on.")]
        [SerializeField] private LayerMask buildingPlacementLayers;
        [Tooltip("Maximum distance from the camera the player can build.")]
        [SerializeField] private float maxBuildDistance = 10f;
        [Tooltip("Expected size of a tile (used for positioning).")]
        [SerializeField] private float tileSize = 2f;

        [Header("Tuning & Debugging")]
        [Tooltip("How directly player must look at a socket (dot product). Lower is more forgiving.")]
        [SerializeField, Range(0.7f, 0.99f)] private float socketSelectionAngle = 0.9f;
        [Tooltip("Small vertical offset when placing buildings on top of surfaces.")]
        [SerializeField] private float buildingYOffset = 0.01f;

        // --- Private State ---
        private GameObject buildPreview;
        private BuildingData currentBuildingData;
        private bool isBuildMode = false;
        private bool canPlace = false;
        private Transform lastHitSocket;
        private Transform lastHitSurface;
        private Collider previewCollider;

        // --- Material Handling ---
        private static readonly int ColorPropertyID = Shader.PropertyToID("_BaseColor"); // URP Lit/SimpleLit property
        private List<Renderer> previewRenderers = new List<Renderer>();
        private Dictionary<Renderer, Material[]> originalMaterials = new Dictionary<Renderer, Material[]>();
        private List<Material> previewMaterialInstances = new List<Material>();

        public bool IsBuildMode => isBuildMode;

        // --- Input Action Callbacks ---
        public void OnAttack(InputValue value)
        {
            if (isBuildMode && value.isPressed && canPlace) PlaceItem();
        }

        public void OnCancelPlacement(InputValue value)
        {
            if (isBuildMode && value.isPressed) CancelBuildMode();
        }

        // --- Unity Methods ---
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Update()
        {
            if (isBuildMode && buildPreview != null) UpdatePreviewPositionAndValidation();
        }

        // --- Public API ---
        public void SelectBuildingToPlace(BuildingData data)
        {
            if (data == null) { CancelBuildMode(); return; }

            isBuildMode = true;
            currentBuildingData = data;
            if (buildPreview != null) Destroy(buildPreview);

            buildPreview = Instantiate(currentBuildingData.buildingPrefab);
            previewRenderers.Clear();
            previewRenderers.AddRange(buildPreview.GetComponentsInChildren<Renderer>());
            previewCollider = buildPreview.GetComponentInChildren<Collider>();

            // --- Configure Preview Object ---
            if (buildPreview.TryGetComponent<Rigidbody>(out var rb))
            {
                rb.isKinematic = true;
                rb.detectCollisions = false;
            }
            if (previewCollider != null)
            {
                previewCollider.enabled = false; // Disable collider to prevent physics interactions
            }
            foreach (var script in buildPreview.GetComponentsInChildren<MonoBehaviour>())
            {
                if (!(script is FactoryTile)) script.enabled = false;
            }
            SetLayerRecursively(buildPreview, LayerMask.NameToLayer("Ignore Raycast"));
        }

        public void CancelBuildMode()
        {
            RevertPreviewMaterials();

            if (buildPreview != null) Destroy(buildPreview);
            isBuildMode = false;
            currentBuildingData = null;
            lastHitSocket = null;
            lastHitSurface = null;
            canPlace = false;
            previewCollider = null;
        }

        // --- Core Logic ---
        private void PlaceItem()
        {
            if (!canPlace || currentBuildingData == null || FactoryManager.Instance == null) return;
            if (!ResourceManager.Instance.HasResource(currentBuildingData.costType, currentBuildingData.costAmount))
            { Debug.Log($"Not enough {currentBuildingData.costType}!"); return; }

            ResourceManager.Instance.SpendResource(currentBuildingData.costType, currentBuildingData.costAmount);
            GameObject newObject = Instantiate(currentBuildingData.buildingPrefab, buildPreview.transform.position, buildPreview.transform.rotation);
            bool isFactoryTile = newObject.GetComponent<FactoryTile>() != null;

            if (isFactoryTile) HandleTilePlacement(newObject);
            else HandleBuildingPlacement(newObject);
        }

        private void HandleTilePlacement(GameObject newTileObject)
        {
            FactoryTile newTileScript = newTileObject.GetComponent<FactoryTile>();
            MobileFactory playerFactory = FactoryManager.Instance.PlayerFactory;

            if (lastHitSocket == null || playerFactory == null || newTileScript == null)
            { Debug.LogError("Tile placement failed: Missing socket, factory, or FactoryTile script.", newTileObject); Destroy(newTileObject); ResourceManager.Instance.AddResource(currentBuildingData.costType, currentBuildingData.costAmount); return; }

            Rigidbody anchorRigidbody = lastHitSocket.GetComponentInParent<Rigidbody>();
            if (anchorRigidbody == null)
            { Debug.LogError($"Tile placement failed: Could not find Rigidbody on parent of socket '{lastHitSocket.name}'.", lastHitSocket); Destroy(newTileObject); ResourceManager.Instance.AddResource(currentBuildingData.costType, currentBuildingData.costAmount); return; }

            ConfigurableJoint joint = newTileObject.AddComponent<ConfigurableJoint>();
            joint.connectedBody = anchorRigidbody;
            joint.xMotion = ConfigurableJointMotion.Locked; joint.yMotion = ConfigurableJointMotion.Locked; joint.zMotion = ConfigurableJointMotion.Locked;
            joint.angularXMotion = ConfigurableJointMotion.Limited; joint.angularYMotion = ConfigurableJointMotion.Limited; joint.angularZMotion = ConfigurableJointMotion.Limited;
            var slerpDrive = new JointDrive { positionSpring = 2000f, positionDamper = 100f, maximumForce = float.MaxValue };
            joint.slerpDrive = slerpDrive;
            joint.projectionMode = JointProjectionMode.PositionAndRotation; joint.projectionDistance = 0.01f; joint.projectionAngle = 1.0f;
            joint.configuredInWorldSpace = false;

            newTileObject.transform.SetParent(playerFactory.transform, true);
            playerFactory.RegisterTile(newTileScript);
            lastHitSocket.gameObject.SetActive(false);

            foreach (Socket socket in newTileObject.GetComponentsInChildren<Socket>(true))
            {
                bool connectsBack = false;
                Vector3 dirToAnchor = (anchorRigidbody.transform.position - newTileObject.transform.position).normalized;
                Vector3 socketForward = (socket.transform.position - newTileObject.transform.position).normalized;
                if (Vector3.Dot(dirToAnchor, socketForward) > 0.95f) connectsBack = true;
                socket.gameObject.SetActive(!connectsBack);
            }
            playerFactory.GetComponent<FactoryNavMeshUpdater>()?.UpdateNavMesh();
        }

        private void HandleBuildingPlacement(GameObject newBuilding)
        {
            if (lastHitSurface == null)
            { Debug.LogError("Building placement failed: No valid surface detected.", newBuilding); Destroy(newBuilding); ResourceManager.Instance.AddResource(currentBuildingData.costType, currentBuildingData.costAmount); return; }

            newBuilding.transform.SetParent(lastHitSurface, true);
            foreach (var script in newBuilding.GetComponentsInChildren<MonoBehaviour>(true))
            { script.enabled = true; }
        }

        private void UpdatePreviewPositionAndValidation()
        {
            canPlace = false;
            lastHitSocket = null;
            lastHitSurface = null;

            Ray centerRay = mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
            bool isFactoryTilePreview = buildPreview.GetComponent<FactoryTile>() != null;

            if (isFactoryTilePreview)
            {
                Transform bestSocket = FindBestSocket(centerRay);
                if (bestSocket != null)
                {
                    lastHitSocket = bestSocket;
                    Transform parentRbTransform = bestSocket.GetComponentInParent<Rigidbody>()?.transform;
                    if (parentRbTransform != null)
                    {
                        Vector3 directionFromParentCenter = (bestSocket.position - parentRbTransform.position).normalized;
                        Vector3 targetPosition = parentRbTransform.position + (directionFromParentCenter * tileSize);
                        Quaternion targetRotation = parentRbTransform.rotation;
                        buildPreview.transform.position = targetPosition;
                        buildPreview.transform.rotation = targetRotation;
                        canPlace = true;
                    }
                    else
                    {
                        PlacePreviewAtRayPoint(centerRay);
                        Debug.LogWarning("Found socket but its parent Rigidbody was null!");
                    }
                }
                else
                {
                    PlacePreviewAtRayPoint(centerRay);
                }
            }
            else
            {
                PlacePreviewOnSurface(centerRay);
            }
            SetPreviewMaterial();
        }

        private void PlacePreviewOnSurface(Ray ray)
        {
            if (Physics.Raycast(ray, out RaycastHit hit, maxBuildDistance, buildingPlacementLayers))
            {
                lastHitSurface = hit.transform;
                FactoryTile hitTile = hit.collider.GetComponentInParent<FactoryTile>();

                Vector3 surfacePoint = hit.point;
                Vector3 surfaceNormal = hit.normal;
                Quaternion surfaceRotation = hit.transform.rotation;

                if (hitTile != null)
                {
                    surfacePoint = hitTile.transform.position;
                    surfaceNormal = hitTile.transform.up;
                    surfaceRotation = hitTile.transform.rotation;
                    lastHitSurface = hitTile.transform;
                }

                float bottomYOffset = 0f;
                if (previewCollider != null)
                {
                    Quaternion initialRot = buildPreview.transform.rotation;
                    buildPreview.transform.rotation = Quaternion.identity;
                    float minYLocal = buildPreview.transform.InverseTransformPoint(previewCollider.bounds.min).y;
                    buildPreview.transform.rotation = initialRot;
                    bottomYOffset = -minYLocal * buildPreview.transform.localScale.y;
                }

                Vector3 targetPosition = surfacePoint + surfaceNormal * (bottomYOffset + buildingYOffset);
                Quaternion targetRotation = (hitTile != null) ?
                    surfaceRotation : // Align exactly with tile
                    Quaternion.LookRotation(Vector3.ProjectOnPlane(mainCamera.transform.forward, surfaceNormal).normalized, surfaceNormal); // Align to other surfaces

                buildPreview.transform.position = targetPosition;
                buildPreview.transform.rotation = targetRotation;
                canPlace = true;
            }
            else
            {
                PlacePreviewAtRayPoint(ray);
            }
        }

        private void PlacePreviewAtRayPoint(Ray ray)
        {
            buildPreview.transform.position = ray.GetPoint(maxBuildDistance * 0.75f);
            canPlace = false;
        }

        private Transform FindBestSocket(Ray viewRay)
        {
            Transform bestSocket = null;
            float bestScore = -1f;
            if (FactoryManager.Instance?.PlayerFactory == null) return null;

            if (Physics.Raycast(viewRay, out RaycastHit hit, maxBuildDistance, factoryTileLayer))
            {
                FactoryTile hitTile = hit.collider.GetComponentInParent<FactoryTile>();
                if (hitTile != null)
                {
                    Vector3 playerLookDir = mainCamera.transform.forward;
                    foreach (var socket in hitTile.GetComponentsInChildren<Socket>())
                    {
                        if (!socket.gameObject.activeInHierarchy) continue;
                        Vector3 toSocketDir = (socket.transform.position - viewRay.origin).normalized;
                        float dot = Vector3.Dot(playerLookDir, toSocketDir);
                        if (dot > socketSelectionAngle)
                        {
                            float distSqr = (socket.transform.position - viewRay.origin).sqrMagnitude;
                            float score = dot / (1f + distSqr);
                            if (score > bestScore)
                            {
                                bestScore = score;
                                bestSocket = socket.transform;
                            }
                        }
                    }
                }
            }
            return bestSocket;
        }

        private void SetPreviewMaterial()
        {
            if (previewRenderers.Count == 0) return;

            if (previewMaterialInstances.Count == 0)
            {
                originalMaterials.Clear();
                foreach (var renderer in previewRenderers)
                {
                    if (renderer != null)
                    {
                        originalMaterials[renderer] = renderer.sharedMaterials;
                        previewMaterialInstances.AddRange(renderer.materials);
                    }
                }
            }

            // Fallback if materials don't have the color property
            bool usedTintColor = false;
            Color tintColor = canPlace ? new Color(0.5f, 1.0f, 0.5f, 0.75f) : new Color(1.0f, 0.5f, 0.5f, 0.75f);

            foreach (Material matInstance in previewMaterialInstances)
            {
                if (matInstance.HasProperty(ColorPropertyID))
                {
                    matInstance.SetColor(ColorPropertyID, tintColor);
                    usedTintColor = true;
                }
            }

            // If tinting failed for all materials, use the simple swap method as a fallback
            if (!usedTintColor)
            {
                Material materialToApply = canPlace ? previewValidMaterial : previewInvalidMaterial;
                if (materialToApply == null) return;
                foreach (var renderer in previewRenderers)
                {
                    if (renderer != null)
                    {
                        var mats = new Material[renderer.sharedMaterials.Length];
                        for (int i = 0; i < mats.Length; i++) mats[i] = materialToApply;
                        renderer.materials = mats;
                    }
                }
            }
        }

        private void RevertPreviewMaterials()
        {
            foreach (var kvp in originalMaterials)
            {
                Renderer renderer = kvp.Key;
                Material[] originalMats = kvp.Value;
                if (renderer != null)
                {
                    renderer.sharedMaterials = originalMats;
                }
            }
            originalMaterials.Clear();
            previewMaterialInstances.Clear();
            previewRenderers.Clear();
        }

        private void SetLayerRecursively(GameObject obj, int layer)
        {
            obj.layer = layer;
            foreach (Transform child in obj.transform) SetLayerRecursively(child.gameObject, layer);
        }
    }
}
