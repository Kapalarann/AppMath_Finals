using UnityEngine;
using System.Collections.Generic;

/// GUIDE: Setting Up a 3D Quad with Normals and Matrices
/// This guide demonstrates how to procedurally create a quad mesh in Unity
/// with proper normals and matrix transformations for rendering.

public class QuadSetupGuide : MonoBehaviour
{
    // STEP 1: Required Components
    public Material material;  // Assign a material in the Inspector

    private Mesh quadMesh;
    private List<Matrix4x4> matrices = new List<Matrix4x4>();

    // Quad dimensions
    public float width = 1f;
    public float height = 1f;

    void Start()
    {
        // STEP 2: Create the quad mesh
        CreateQuadMesh();

        // STEP 3: Create transformation matrices for instances
        CreateQuadInstances();
    }

    // STEP 2 IMPLEMENTATION: Create a Quad Mesh
    void CreateQuadMesh()
    {
        quadMesh = new Mesh();
        quadMesh.name = "ProceduralQuad";

        // Define 4 vertices for the quad (in local space)
        // Quad is centered at origin, facing forward (+Z direction)
        Vector3[] vertices = new Vector3[4]
        {
            new Vector3(-width/2, -height/2, 0),  // Bottom-left  [0]
            new Vector3( width/2, -height/2, 0),  // Bottom-right [1]
            new Vector3(-width/2,  height/2, 0),  // Top-left     [2]
            new Vector3( width/2,  height/2, 0)   // Top-right    [3]
        };

        // Define 2 triangles (6 indices) to form the quad
        // Triangle winding order matters for face direction!
        // Counter-clockwise winding = front face
        int[] triangles = new int[6]
        {
            0, 2, 1,  // First triangle: bottom-left, top-left, bottom-right
            1, 2, 3   // Second triangle: bottom-right, top-left, top-right
        };

        // Define UV coordinates for texture mapping
        Vector2[] uvs = new Vector2[4]
        {
            new Vector2(0, 0),  // Bottom-left
            new Vector2(1, 0),  // Bottom-right
            new Vector2(0, 1),  // Top-left
            new Vector2(1, 1)   // Top-right
        };

        // IMPORTANT: Manually define normals for proper lighting
        // All normals point forward (+Z) for a front-facing quad
        Vector3[] normals = new Vector3[4]
        {
            Vector3.forward,  // Normal for vertex 0
            Vector3.forward,  // Normal for vertex 1
            Vector3.forward,  // Normal for vertex 2
            Vector3.forward   // Normal for vertex 3
        };

        // Assign mesh data
        quadMesh.vertices = vertices;
        quadMesh.triangles = triangles;
        quadMesh.uv = uvs;
        quadMesh.normals = normals;  // Explicitly set normals

        // Alternative: Let Unity calculate normals automatically
        // quadMesh.RecalculateNormals();

        // Recalculate bounds for proper culling
        quadMesh.RecalculateBounds();
    }

    // STEP 3 IMPLEMENTATION: Create Transformation Matrices
    void CreateQuadInstances()
    {
        // Example 1: Create a quad at origin with no rotation
        Vector3 position1 = new Vector3(0, 0, 0);
        Quaternion rotation1 = Quaternion.identity;
        Vector3 scale1 = Vector3.one;
        Matrix4x4 matrix1 = Matrix4x4.TRS(position1, rotation1, scale1);
        matrices.Add(matrix1);

        // Example 2: Create a quad rotated 45 degrees on Y-axis
        Vector3 position2 = new Vector3(3, 0, 0);
        Quaternion rotation2 = Quaternion.Euler(0, 45, 0);
        Vector3 scale2 = Vector3.one;
        Matrix4x4 matrix2 = Matrix4x4.TRS(position2, rotation2, scale2);
        matrices.Add(matrix2);

        // Example 3: Create a scaled and positioned quad
        Vector3 position3 = new Vector3(-3, 0, 0);
        Quaternion rotation3 = Quaternion.identity;
        Vector3 scale3 = new Vector3(2, 0.5f, 1);  // Width=2, Height=0.5
        Matrix4x4 matrix3 = Matrix4x4.TRS(position3, rotation3, scale3);
        matrices.Add(matrix3);
    }

    void Update()
    {
        // STEP 4: Render all quad instances
        RenderQuads();
    }

    // STEP 4 IMPLEMENTATION: Render Using Instancing
    void RenderQuads()
    {
        if (quadMesh == null || material == null) return;

        // Convert list to array for GPU instancing
        Matrix4x4[] matrixArray = matrices.ToArray();

        // Draw all instances in one batch (max 1023 per batch)
        Graphics.DrawMeshInstanced(quadMesh, 0, material, matrixArray);
    }

    // UTILITY: Add a new quad at runtime
    public void AddQuad(Vector3 position, Quaternion rotation, Vector3 scale)
    {
        Matrix4x4 matrix = Matrix4x4.TRS(position, rotation, scale);
        matrices.Add(matrix);
    }

    // UTILITY: Decompose a matrix back to position/rotation/scale
    public void DecomposeMatrix(Matrix4x4 matrix, out Vector3 position,
                                out Quaternion rotation, out Vector3 scale)
    {
        position = matrix.GetPosition();
        rotation = matrix.rotation;
        scale = matrix.lossyScale;
    }
}

/* ============================================================================
   KEY CONCEPTS EXPLAINED:
   ============================================================================

   1. VERTICES:
   - Define the corner points of your quad in 3D space
   - Use local coordinates (relative to origin)
   - Order matters for triangle winding

   2. TRIANGLES:
   - Define which vertices form triangles (using indices)
   - Counter-clockwise winding = front face
   - A quad needs 2 triangles (6 indices total)

   3. NORMALS:
   - Perpendicular vectors from the surface
   - Critical for proper lighting calculations
   - Can be set manually or calculated with RecalculateNormals()
   - For a flat quad facing +Z, all normals should be (0, 0, 1)

   4. UV COORDINATES:
   - Map 2D texture space (0-1) to 3D vertices
   - (0,0) = bottom-left of texture
   - (1,1) = top-right of texture

   5. MATRIX4x4 (TRS):
   - T = Translation (position in world space)
   - R = Rotation (orientation as Quaternion)
   - S = Scale (size multiplier)
   - Combines all transformations into one matrix
   - Used by GPU for efficient instanced rendering

   6. INSTANCED RENDERING:
   - Graphics.DrawMeshInstanced() renders multiple copies efficiently
   - Each instance uses the same mesh but different matrix
   - GPU processes all instances in parallel
   - Maximum 1023 instances per batch

   ============================================================================
   USAGE IN CLASS:
   ============================================================================

   1. Attach this script to an empty GameObject
   2. Create a material and assign it in the Inspector
   3. Press Play to see three quads rendered
   4. Modify CreateQuadInstances() to add more quads

   TO CREATE DIFFERENT SHAPES:
   - Increase vertex count (4 for quad, 8 for cube, etc.)
   - Update triangle indices accordingly
   - Recalculate or manually set normals for each face

   ============================================================================
*/
