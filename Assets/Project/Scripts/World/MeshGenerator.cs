// /Assets/Project/Scripts/World/MeshGenerator.cs
using UnityEngine;

namespace AutoForge.World
{
    /// <summary>
    /// A simple data container for mesh components.
    /// </summary>
    public struct MeshData
    {
        public Vector3[] vertices;
        public int[] triangles;
        public Vector2[] uvs;
        public Color[] colors; // Used for splatmap weights
    }

    public static class MeshGenerator
    {
        /// <summary>
        /// Generates mesh data from a heightmap and splatmap (colors).
        /// Includes fixes for triangle winding and vertex positioning at chunk boundaries.
        /// </summary>
        public static MeshData GenerateMesh(float[,] heightMap, Color[,] splatMap, int chunkSize, int chunkHeight)
        {
            int width = heightMap.GetLength(0);  // e.g., chunkResolution = 129
            int height = heightMap.GetLength(1); // e.g., chunkResolution = 129

            // Safety check for valid dimensions
            if (width <= 1 || height <= 1)
            {
                Debug.LogError("[MeshGenerator] HeightMap dimensions too small! Cannot generate mesh.");
                return new MeshData
                { // Return empty data to avoid errors downstream
                    vertices = new Vector3[0],
                    triangles = new int[0],
                    uvs = new Vector2[0],
                    colors = new Color[0]
                };
            }

            // Calculate spacing between vertices based on chunk size and resolution
            // If width = 129, there are 128 segments. chunkSize / 128 = spacing.
            float vertexSpacing = (float)chunkSize / (width - 1);

            MeshData meshData = new MeshData
            {
                vertices = new Vector3[width * height],
                triangles = new int[(width - 1) * (height - 1) * 6], // Triangles based on number of quads
                uvs = new Vector2[width * height],
                colors = new Color[width * height]
            };

            int triangleIndex = 0;
            int vertexIndex = 0;

            for (int y = 0; y < height; y++) // Iterate through Z axis vertices
            {
                for (int x = 0; x < width; x++) // Iterate through X axis vertices
                {
                    // Get height from noise map (normalized 0-1) and scale by chunkHeight
                    float yPos = heightMap[x, y] * chunkHeight;

                    // --- VERTEX POSITION CALCULATION (Centered around Pivot) ---
                    // Calculate local position relative to chunk's corner (0,0) based on index
                    float localX = x * vertexSpacing;
                    float localZ = y * vertexSpacing;

                    // Shift position so the mesh is centered around the chunk GameObject's origin (pivot)
                    // This helps ensure boundary vertices align correctly.
                    meshData.vertices[vertexIndex] = new Vector3(
                        localX - (chunkSize / 2f), // Centered X
                        yPos,                      // Height
                        localZ - (chunkSize / 2f)  // Centered Z
                    );
                    // --- END VERTEX POSITION CALCULATION ---

                    // --- UV CALCULATION (Corrected 0-1 range) ---
                    // Map vertex index (0 to width-1) to UV coordinate (0 to 1)
                    meshData.uvs[vertexIndex] = new Vector2(x / (float)(width - 1), y / (float)(height - 1));
                    // --- END UV CALCULATION ---

                    // Assign vertex color for splatmap shader
                    meshData.colors[vertexIndex] = splatMap[x, y];

                    // --- TRIANGLE GENERATION ---
                    // Only create triangles if we are not at the last row or column
                    if (x < width - 1 && y < height - 1)
                    {
                        // Get indices of the four corners of the current quad
                        int topLeft = vertexIndex;               // Current vertex (x, y)
                        int topRight = vertexIndex + 1;           // Vertex to the right (x+1, y)
                        int bottomLeft = vertexIndex + width;       // Vertex below (x, y+1)
                        int bottomRight = vertexIndex + width + 1;   // Vertex diagonal (x+1, y+1)

                        // --- CORRECTED TRIANGLE WINDING ORDER ---
                        // Creates two triangles for the quad using counter-clockwise winding (standard for Unity)

                        // Triangle 1: Top-Left -> Bottom-Left -> Top-Right
                        meshData.triangles[triangleIndex + 0] = topLeft;
                        meshData.triangles[triangleIndex + 1] = bottomLeft;
                        meshData.triangles[triangleIndex + 2] = topRight;

                        // Triangle 2: Top-Right -> Bottom-Left -> Bottom-Right
                        meshData.triangles[triangleIndex + 3] = topRight;
                        meshData.triangles[triangleIndex + 4] = bottomLeft;
                        meshData.triangles[triangleIndex + 5] = bottomRight;
                        // --- END TRIANGLE WINDING ORDER ---

                        triangleIndex += 6; // Move to the next pair of triangles
                    }
                    vertexIndex++; // Move to the next vertex index
                }
            }
            return meshData; // Return the completed mesh data container
        }

        /// <summary>
        /// Applies the generated MeshData to actual Mesh components.
        /// Includes physics fix by toggling collider enabled state.
        /// </summary>
        public static void ApplyToMesh(Mesh mesh, MeshCollider meshCollider, MeshData meshData)
        {
            // --- Safety Check: Ensure meshData is valid ---
            if (meshData.vertices == null || meshData.vertices.Length == 0 ||
                meshData.triangles == null || meshData.triangles.Length == 0)
            {
                Debug.LogWarning($"<color=yellow>[MeshGenerator Warning]</color> Attempted to apply mesh with zero vertices or triangles to {meshCollider?.gameObject.name ?? "Unknown Object"}. Skipping mesh update.");
                // Optionally clear the existing mesh to prevent rendering artifacts
                // mesh.Clear();
                // if (meshCollider != null) meshCollider.sharedMesh = null;
                return; // Stop execution if data is invalid
            }

            // --- Apply Mesh Data ---
            mesh.Clear(); // Clear previous mesh data
            mesh.vertices = meshData.vertices;
            mesh.triangles = meshData.triangles;
            mesh.uv = meshData.uvs;
            mesh.colors = meshData.colors; // For splatmap shader

            mesh.RecalculateNormals(); // Calculate lighting normals based on triangles
            mesh.RecalculateBounds(); // Calculate bounding box for rendering culling

            // --- Apply to Physics Collider (Bulletproof Fix) ---
            if (meshCollider == null)
            {
                Debug.LogError($"<color=red><b>[MeshGenerator ERROR]</b></color> MeshCollider is NULL on GameObject using mesh '{mesh.name}'. Cannot apply physics mesh.");
                return;
            }

            meshCollider.enabled = false; // Disable collider to clear old physics state
            meshCollider.sharedMesh = null; // Explicitly clear reference to old mesh
            meshCollider.sharedMesh = mesh; // Assign the newly generated mesh
            meshCollider.enabled = true; // Re-enable collider to force physics update

            // Non-spammy success log (keep commented unless needed for deep debugging)
            // Debug.Log($"<color=lime>[MeshGenerator]</color> SUCCESS: Applied mesh and toggled collider on {meshCollider.gameObject.name}. Verts: {meshData.vertices.Length}, Tris: {meshData.triangles.Length}");
        }
    }
}