using UnityEngine;
using UnityEngine.InputSystem;
using AutoForge.Core;

namespace AutoForge.Player
{
    public class PlayerBuilder : MonoBehaviour
    {
        public static PlayerBuilder Instance { get; private set; }

        [Header("Required References")]
        [SerializeField] private Camera mainCamera;

        // --- UPDATED MATERIAL FIELD ---
        [Tooltip("A transparent material used as a template for the preview.")]
        [SerializeField] private Material transparentPreviewMaterial;
        // --- END UPDATE ---

        [Header("Placement Settings")]
        [SerializeField] private LayerMask groundLayer;
        [SerializeField] private float maxBuildDistance = 100f;
        [SerializeField] private float placementYOffset = 0.5f;

        private GameObject buildPreview;
        private BuildingData currentBuildingData;
        private bool isBuildMode = false;
        public bool IsInBuildMode => isBuildMode;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        public void OnAttack(InputValue value)
        {
            if (isBuildMode && value.isPressed)
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
            SetLayerRecursively(buildPreview, LayerMask.NameToLayer("Ignore Raycast"));

            // --- NEW TRANSPARENCY LOGIC ---
            if (transparentPreviewMaterial != null)
            {
                Renderer[] renderers = buildPreview.GetComponentsInChildren<Renderer>();
                foreach (Renderer renderer in renderers)
                {
                    Material[] originalMaterials = renderer.materials;
                    Material[] transparentMaterials = new Material[originalMaterials.Length];

                    for (int i = 0; i < originalMaterials.Length; i++)
                    {
                        transparentMaterials[i] = new Material(transparentPreviewMaterial);
                        transparentMaterials[i].mainTexture = originalMaterials[i].mainTexture;
                        transparentMaterials[i].color = new Color(
                            originalMaterials[i].color.r,
                            originalMaterials[i].color.g,
                            originalMaterials[i].color.b,
                            transparentPreviewMaterial.color.a);
                    }
                    renderer.materials = transparentMaterials;
                }
            }
            // --- END NEW LOGIC ---

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
        }

        private void PlaceBuilding()
        {
            if (buildPreview == null || currentBuildingData == null) return;
            if (ResourceManager.Instance.HasResource(currentBuildingData.costType, currentBuildingData.costAmount))
            {
                ResourceManager.Instance.SpendResource(currentBuildingData.costType, currentBuildingData.costAmount);
                Instantiate(currentBuildingData.buildingPrefab, buildPreview.transform.position, buildPreview.transform.rotation);
                CancelBuildMode();
            }
            else
            {
                Debug.Log($"Not enough {currentBuildingData.costType.resourceName}!");
            }
        }

        private void MoveAndAlignPreview()
        {
            if (mainCamera == null) return;
            Ray ray = mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
            if (Physics.Raycast(ray, out RaycastHit hit, maxBuildDistance, groundLayer))
            {
                Vector3 position = hit.point + new Vector3(0, placementYOffset, 0);
                buildPreview.transform.position = position;
                buildPreview.transform.rotation = Quaternion.FromToRotation(Vector3.up, hit.normal);
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