# 3D Quad Setup Guide with Normals and Matrices

A step-by-step tutorial for creating procedural 3D quads in Unity using custom mesh generation, normals, and transformation matrices.

---

## Table of Contents

1. [Prerequisites](#prerequisites)
2. [What You'll Learn](#what-youll-learn)
3. [Step-by-Step Setup](#step-by-step-setup)
4. [Understanding the Code](#understanding-the-code)
5. [Experimentation Ideas](#experimentation-ideas)
6. [Common Issues](#common-issues)

---

## Prerequisites

- Unity 2020.3 or later
- Basic understanding of C# and Unity
- Understanding of 3D coordinate systems (X, Y, Z axes)

---

## What You'll Learn

- How to create a mesh from scratch using vertices and triangles
- How normals affect lighting and face direction
- How to use Matrix4x4 for transformations (position, rotation, scale)
- How to render multiple instances efficiently using GPU instancing

---

## Step-by-Step Setup

### Step 1: Create a New Scene

1. Open your Unity project
2. Create a new scene or use an existing one
3. Delete or disable the default cube (if present)

### Step 2: Create an Empty GameObject

1. In the Hierarchy, right-click and select `Create Empty`
2. Rename it to `QuadGenerator`
3. Reset its transform (position: 0,0,0, rotation: 0,0,0, scale: 1,1,1)

### Step 3: Attach the Script

1. Locate the `QuadSetupGuide.cs` script in `Assets/Scripts/`
2. Drag and drop it onto the `QuadGenerator` GameObject
   - OR select `QuadGenerator` and click `Add Component`, then search for "QuadSetupGuide"

### Step 4: Create a Material

1. In the Project window, right-click in `Assets/`
2. Select `Create > Material`
3. Name it `QuadMaterial`
4. Choose a shader (Standard, URP/Lit, or your preferred shader)
5. Set a color or texture to make the quads visible

### Step 5: Assign the Material

1. Select the `QuadGenerator` GameObject
2. In the Inspector, find the `QuadSetupGuide` component
3. Drag the `QuadMaterial` into the `Material` slot

### Step 6: Adjust Settings (Optional)

In the Inspector, you can modify:
- **Width**: Width of the quad (default: 1)
- **Height**: Height of the quad (default: 1)

### Step 7: Run the Scene

1. Press the Play button
2. You should see three quads:
   - One at the origin (center)
   - One rotated 45° to the right
   - One scaled and positioned to the left

### Step 8: Adjust Camera Position

1. Select the Main Camera
2. Position it to see the quads clearly
   - Suggested position: (0, 2, -5)
   - Suggested rotation: (10, 0, 0)

---

## Understanding the Code

### Part 1: Mesh Structure

A mesh consists of three essential components:

#### Vertices (Corner Points)

```csharp
Vector3[] vertices = new Vector3[4]
{
    new Vector3(-width/2, -height/2, 0),  // Bottom-left  [0]
    new Vector3( width/2, -height/2, 0),  // Bottom-right [1]
    new Vector3(-width/2,  height/2, 0),  // Top-left     [2]
    new Vector3( width/2,  height/2, 0)   // Top-right    [3]
};
```

- Defines 4 corner points in 3D space
- Centered at origin (0, 0, 0)
- Facing forward along the Z-axis

#### Triangles (Face Definition)

```csharp
int[] triangles = new int[6]
{
    0, 2, 1,  // First triangle
    1, 2, 3   // Second triangle
};
```

- Uses vertex indices to form triangles
- Counter-clockwise winding = front face
- 2 triangles form 1 quad

#### Normals (Surface Direction)

```csharp
Vector3[] normals = new Vector3[4]
{
    Vector3.forward,  // (0, 0, 1)
    Vector3.forward,
    Vector3.forward,
    Vector3.forward
};
```

- Perpendicular vectors pointing away from surface
- Essential for lighting calculations
- All point forward for a flat quad facing +Z

### Part 2: Matrix Transformations

A Matrix4x4 combines three transformations:

```csharp
Matrix4x4 matrix = Matrix4x4.TRS(position, rotation, scale);
```

- **T** (Translation): Where to position the quad
- **R** (Rotation): How to rotate the quad (Quaternion)
- **S** (Scale): How to resize the quad

#### Example: Position a Quad

```csharp
Vector3 position = new Vector3(5, 2, 0);      // 5 units right, 2 units up
Quaternion rotation = Quaternion.identity;     // No rotation
Vector3 scale = Vector3.one;                   // Original size
Matrix4x4 matrix = Matrix4x4.TRS(position, rotation, scale);
```

#### Example: Rotate a Quad

```csharp
Vector3 position = new Vector3(0, 0, 0);
Quaternion rotation = Quaternion.Euler(0, 90, 0);  // 90° around Y-axis
Vector3 scale = Vector3.one;
Matrix4x4 matrix = Matrix4x4.TRS(position, rotation, scale);
```

#### Example: Scale a Quad

```csharp
Vector3 position = new Vector3(0, 0, 0);
Quaternion rotation = Quaternion.identity;
Vector3 scale = new Vector3(2, 3, 1);  // 2x width, 3x height
Matrix4x4 matrix = Matrix4x4.TRS(position, rotation, scale);
```

### Part 3: Instanced Rendering

```csharp
Graphics.DrawMeshInstanced(quadMesh, 0, material, matrixArray);
```

- Renders multiple copies of the same mesh efficiently
- Each instance uses a different transformation matrix
- GPU processes all instances in parallel
- Much faster than creating individual GameObjects

---

## Experimentation Ideas

### Experiment 1: Create a Grid of Quads

Modify `CreateQuadInstances()`:

```csharp
void CreateQuadInstances()
{
    for (int x = 0; x < 5; x++)
    {
        for (int y = 0; y < 5; y++)
        {
            Vector3 position = new Vector3(x * 2, y * 2, 0);
            Quaternion rotation = Quaternion.identity;
            Vector3 scale = Vector3.one;
            Matrix4x4 matrix = Matrix4x4.TRS(position, rotation, scale);
            matrices.Add(matrix);
        }
    }
}
```

### Experiment 2: Animate Rotation in Update()

Add to the `Update()` method:

```csharp
void Update()
{
    // Rotate the first quad
    if (matrices.Count > 0)
    {
        DecomposeMatrix(matrices[0], out Vector3 pos, out Quaternion rot, out Vector3 scale);
        rot *= Quaternion.Euler(0, 0, 50 * Time.deltaTime);  // Rotate around Z-axis
        matrices[0] = Matrix4x4.TRS(pos, rot, scale);
    }

    RenderQuads();
}
```

### Experiment 3: Different Facing Directions

Create quads facing different directions by changing normals and rotation:

```csharp
// Quad facing right (+X)
Quaternion rotation = Quaternion.Euler(0, 90, 0);
Vector3[] normals = new Vector3[4]
{
    Vector3.right, Vector3.right, Vector3.right, Vector3.right
};
```

### Experiment 4: Wave Effect

Create a wave pattern with quads:

```csharp
void CreateQuadInstances()
{
    for (int i = 0; i < 20; i++)
    {
        float x = i * 1.5f;
        float y = Mathf.Sin(i * 0.5f) * 2;  // Sine wave
        Vector3 position = new Vector3(x, y, 0);
        Quaternion rotation = Quaternion.identity;
        Vector3 scale = Vector3.one;
        Matrix4x4 matrix = Matrix4x4.TRS(position, rotation, scale);
        matrices.Add(matrix);
    }
}
```

---

## Common Issues

### Issue 1: Quads Not Visible

**Possible causes:**
- Material not assigned
- Camera positioned incorrectly
- Quads rendered behind the camera

**Solution:**
- Check the material is assigned in Inspector
- Position camera at (0, 0, -10) looking forward
- Check quad positions are in front of camera

### Issue 2: Quads Are Black

**Possible causes:**
- Normals pointing wrong direction
- No light in the scene
- Wrong shader on material

**Solution:**
- Add a Directional Light to the scene
- Use `quadMesh.RecalculateNormals()` instead of manual normals
- Try an Unlit shader to rule out lighting issues

### Issue 3: Quads Appear Inverted

**Possible causes:**
- Triangle winding order is clockwise instead of counter-clockwise
- Viewing from the back side

**Solution:**
- Reverse triangle indices: `{1, 2, 0, 3, 2, 1}`
- Or flip normals: `Vector3.back` instead of `Vector3.forward`

### Issue 4: Performance Issues with Many Quads

**Solution:**
- GPU instancing is already optimized for many instances
- Ensure you're using one `DrawMeshInstanced` call, not multiple
- Limit to 1023 instances per batch (script handles this)

---

## Key Takeaways

1. **Vertices** define the shape's corner points
2. **Triangles** connect vertices to form surfaces
3. **Normals** determine lighting and face direction
4. **Matrices** combine position, rotation, and scale into one transformation
5. **Instanced rendering** efficiently renders many copies of the same mesh

---

## Next Steps

- Try creating other shapes (hexagon, circle with many triangles)
- Implement dynamic mesh modification
- Add texture coordinates animation
- Create a particle system using quad instances
- Combine multiple mesh types in one scene

---

## Reference

Based on `EnhancedMeshGenerator.cs` lines 96-155 (cube mesh creation)

For more information:
- Unity Mesh API: https://docs.unity3d.com/ScriptReference/Mesh.html
- Matrix4x4: https://docs.unity3d.com/ScriptReference/Matrix4x4.html
- DrawMeshInstanced: https://docs.unity3d.com/ScriptReference/Graphics.DrawMeshInstanced.html
