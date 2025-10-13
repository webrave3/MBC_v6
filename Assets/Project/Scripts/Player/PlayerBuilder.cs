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
        [SerializeField] private Material transparentPreviewMaterial;

        [Header("Placement Settings")]
        [SerializeField] private LayerMask groundLayer;
        [SerializeField] private LayerMask factoryFloorLayer;
        [SerializeField] private float maxBuildDistance = 100f;
        [Tooltip("How close to the center of the screen a socket must be to be selected (in pixels).")]
        [SerializeField] private float socketSelectionRadius = 150f;

        private GameObject buildPreview;
        private BuildingData currentBuildingData;
        private bool isBuildMode = false;
        public bool IsInBuildMode => isBuildMode;

        private bool isPlacingFactoryTile = false;
        private bool canPlace = false;
        private Transform lastHitSocket;

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

            foreach (Collider col in buildPreview.GetComponentsInChildren<Collider>())
            {
                col.isTrigger = true;
            }

            SetLayerRecursively(buildPreview, LayerMask.NameToLayer("Ignore Raycast"));

            // Your original material logic
            if (transparentPreviewMaterial != null)
            {
                Renderer[] renderers = buildPreview.GetComponentsInChildren<Renderer>();
                foreach (Renderer renderer in renderers)
                {
                    Material[] originalMaterials = renderer.materials;
                    Material[] transparentMaterials = new Material[originalMaterials.Length];
                    for (int i = 0; i < originalMaterials.Length; i++)
                    {
                        transparentMaterials[i] = new Material(transparentPreviewMaterial)
                        {
                            mainTexture = originalMaterials[i].mainTexture,
                            color = new Color(originalMaterials[i].color.r, originalMaterials[i].color.g, originalMaterials[i].color.b, transparentPreviewMaterial.color.a)
                        };
                    }
                    renderer.materials = transparentMaterials;
                }
            }

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
            if (buildPreview == null || currentBuildingData == null || !canPlace) return;

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
            else
            {
                Debug.Log($"Not enough {currentBuildingData.costType.resourceName}!");
            }
        }

        private void MoveAndAlignPreview()
        {
            if (mainCamera == null) return;

            if (isPlacingFactoryTile)
            {
                // --- SCREEN-SPACE TARGETING LOGIC ---
                Transform bestSocket = null;
                float closestDist = float.MaxValue;
                Vector2 screenCenter = new Vector2(Screen.width / 2, Screen.height / 2);

                if (FactoryManager.Instance?.MobileFactory != null)
                {
                    var sockets = FactoryManager.Instance.MobileFactory.GetComponentsInChildren<Socket>();
                    foreach (var socket in sockets)
                    {
                        Vector3 screenPoint = mainCamera.WorldToScreenPoint(socket.transform.position);
                        if (screenPoint.z > 0) // Is the socket in front of the camera?
                        {
                            float dist = Vector2.Distance(new Vector2(screenPoint.x, screenPoint.y), screenCenter);
                            if (dist < closestDist && dist < socketSelectionRadius)
                            {
                                closestDist = dist;
                                bestSocket = socket.transform;
                            }
                        }
                    }
                }

                if (bestSocket != null)
                {
                    buildPreview.SetActive(true);
                    canPlace = true;
                    lastHitSocket = bestSocket;

                    // --- THIS IS THE FIX ---
                    // The Tile prefab's size is 1. We offset by half of that (0.5).
                    float tileSize = 1f;
                    Vector3 offset = bestSocket.forward * (tileSize / 2);
                    buildPreview.transform.position = bestSocket.position + offset;
                    // --- END FIX ---

                    buildPreview.transform.rotation = bestSocket.rotation;
                }
                else
                {
                    buildPreview.SetActive(false);
                    canPlace = false;
                    lastHitSocket = null;
                }
            }
            else
            {
                // --- ORIGINAL BUILDING PLACEMENT LOGIC (UNCHANGED) ---
                Ray ray = mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
                LayerMask combinedMask = groundLayer | factoryFloorLayer;
                if (Physics.Raycast(ray, out RaycastHit hit, maxBuildDistance, combinedMask))
                {
                    buildPreview.SetActive(true);
                    canPlace = true;
                    buildPreview.transform.position = hit.point;
                    buildPreview.transform.rotation = Quaternion.FromToRotation(Vector3.up, hit.normal);
                }
                else
                {
                    buildPreview.SetActive(false);
                    canPlace = false;
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