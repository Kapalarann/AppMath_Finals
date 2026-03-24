using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class EnhancedMeshGenerator : MonoBehaviour
{
    [Header("Materials")]
    public Material playerMaterial;
    public Material enemyMaterial;
    public Material boxMaterial;
    public Material hazardMaterial;
    public Material powerupMaterial;

    private List<Matrix4x4> playerMatrices = new List<Matrix4x4>();
    private List<Matrix4x4> enemyMatrices = new List<Matrix4x4>();
    private List<Matrix4x4> boxMatrices = new List<Matrix4x4>();
    private List<Matrix4x4> hazardMatrices = new List<Matrix4x4>();
    private List<Matrix4x4> powerupMatrices = new List<Matrix4x4>();

    private Mesh cubeMesh;
    private List<Matrix4x4> matrices = new List<Matrix4x4>();
    private List<int> colliderIds = new List<int>();

    [Header("Scale Defaults")]
    public float width = 1f;
    public float height = 1f;
    public float depth = 1f;

    [Header("Platforming Stats")]
    public Vector3 playerStartPos = Vector3.zero;
    public float movementSpeed = 5f;
    public float jumpStrength = 10f;
    public float gravity = 9.8f;
    public float inputBuffer = 0.5f;
    private int maxJumps = 1;
    private int jumpsRemaining;
    private float baseGravity;

    [Header("Player Stats")]
    public int maxHealth = 3;
    private int currentHealth;
    private float invincibilityTimer = 0f;
    public float invincibilityDuration = 1.0f;

    private float bufferTimer = 0f;
    private int playerID = -1;
    private Vector3 playerVelocity = Vector3.zero;
    private bool isGrounded = false;

    public PlayerCameraFollow cameraFollow;
    public float constantZPosition = 0f;

    [Header("Level Setup")]
    public PlatformData[] platforms;
    public EnemyData[] enemies;
    public HazardData[] hazards;
    public PowerupData[] powerups;

    private List<int> enemyIds = new List<int>();
    private List<EnemyData> enemyDataList = new List<EnemyData>();
    private List<int> hazardIds = new List<int>();
    private List<int> powerupIds = new List<int>();

    [Header("Powerup Stats")]
    public float gravityReductionScale = 0.5f;

    private float powerupTimer = 0f;
    private PowerupType activeType;

    public float groundY = -20f;
    public float groundWidth = 200f;
    public float groundDepth = 200f;

    void Start()
    {
        SetupCamera();
        CreateCubeMesh();

        CreatePlayer();
        CreateGround();
        SpawnPlatforms();
        SpawnEnemies();
        SpawnHazards();
        SpawnPowerups();

        currentHealth = maxHealth;
        baseGravity = gravity;
    }

    void SetupCamera()
    {
        if (cameraFollow == null)
        {
            Camera mainCamera = Camera.main;
            if (mainCamera != null)
                cameraFollow = mainCamera.GetComponent<PlayerCameraFollow>() ?? mainCamera.gameObject.AddComponent<PlayerCameraFollow>();
            else
            {
                GameObject cameraObj = new GameObject("PlayerCamera");
                cameraObj.AddComponent<Camera>().tag = "MainCamera";
                cameraFollow = cameraObj.AddComponent<PlayerCameraFollow>();
            }
            cameraFollow.offset = new Vector3(0, 0, -15);
            cameraFollow.smoothSpeed = 0.1f;
        }
    }

    void Update()
    {
        UpdateTimers();
        UpdatePlayer();
        UpdateEnemies();
        RenderBoxes();
    }

    void UpdateTimers()
    {
        if (invincibilityTimer > 0) invincibilityTimer -= Time.deltaTime;

        if (powerupTimer > 0)
        {
            powerupTimer -= Time.deltaTime;
            if (powerupTimer <= 0) ResetPowerupEffects();
        }
    }

    void UpdatePlayer()
    {
        if (playerID == -1) return;

        int index = colliderIds.IndexOf(playerID);
        DecomposeMatrix(matrices[index], out Vector3 pos, out Quaternion rot, out Vector3 scale);

        HandleJumpInput();
        ApplyGravity();

        // Horizontal Movement
        float moveInput = (Input.GetKey(KeyCode.A) ? -1 : 0) + (Input.GetKey(KeyCode.D) ? 1 : 0);
        if (MoveAndCollide(ref pos, new Vector3(moveInput * movementSpeed * Time.deltaTime, 0, 0), true))
            return;

        // Vertical Movement
        if (MoveAndCollide(ref pos, new Vector3(0, playerVelocity.y * Time.deltaTime, 0), false))
            return;

        // Finalize Player State
        if (isGrounded && playerVelocity.y <= 0)
        {
            playerVelocity.y = 0;
            jumpsRemaining = maxJumps;
        }

        UpdatePlayerMatrix(index, pos, rot, scale);
    }

    void HandleJumpInput()
    {
        if (Input.GetKeyDown(KeyCode.Space)) bufferTimer = inputBuffer;

        if (bufferTimer > 0f)
        {
            if (isGrounded || jumpsRemaining > 0)
            {
                playerVelocity.y = jumpStrength;
                jumpsRemaining--;
                isGrounded = false;
                bufferTimer = 0f;
            }
            bufferTimer -= Time.deltaTime;
        }
    }

    void ApplyGravity()
    {
        float verticalMult = Input.GetKey(KeyCode.Space) && (playerVelocity.y < 0f) ? 0.5f : 1f;
        playerVelocity.y -= verticalMult * gravity * Time.deltaTime;
    }

    // We use 'ref' for the position so we can modify it and return a bool for the reset status
    bool MoveAndCollide(ref Vector3 currentPos, Vector3 movement, bool isHorizontal)
    {
        Vector3 targetPos = currentPos + movement;

        if (CollisionManager.Instance.CheckCollision(playerID, targetPos, out List<int> hitIds))
        {
            foreach (int id in hitIds)
            {
                if (powerupIds.Contains(id)) CollectPowerup(id);

                if (invincibilityTimer <= 0f && (hazardIds.Contains(id) || enemyIds.Contains(id)))
                {
                    // If HandlePlayerDamage returns true, it means ResetPlayer() was called
                    if (HandlePlayerDamage(id))
                    {
                        return true; // Report that a reset occurred!
                    }
                    return false;
                }
            }

            if (!isHorizontal && playerVelocity.y < 0) isGrounded = true;
            if (!isHorizontal) playerVelocity.y = 0;

            return false; // Collision happened, but no reset
        }

        if (!isHorizontal) isGrounded = false;
        currentPos = targetPos; // Update the position ref
        return false; // Move successful, no reset
    }

    void UpdateEnemies()
    {
        for (int i = 0; i < enemyIds.Count; i++)
        {
            int id = enemyIds[i];
            EnemyData data = enemyDataList[i];
            int index = colliderIds.IndexOf(id);

            DecomposeMatrix(matrices[index], out Vector3 pos, out Quaternion rot, out Vector3 scale);

            // AI Horizontal
            float movementStep = data.currentDir * data.speed * Time.deltaTime;
            Vector3 hTarget = pos + new Vector3(movementStep, 0, 0);

            float relX = hTarget.x - data.spawnPos.x;
            if (relX < data.roamXRange.x || relX > data.roamXRange.y || CheckCollisionAt(id, hTarget))
                data.currentDir *= -1f;
            else
                pos.x = hTarget.x;

            // AI Gravity
            if (data.useGravity)
            {
                data.verticalVelocity -= gravity * Time.deltaTime;
                Vector3 vTarget = pos + new Vector3(0, data.verticalVelocity * Time.deltaTime, 0);
                if (CheckCollisionAt(id, vTarget)) data.verticalVelocity = 0;
                else pos.y = vTarget.y;
            }

            UpdateEnemyMatrix(i, index, pos, rot, scale);
        }
    }

    bool HandlePlayerDamage(int hitId)
    {
        int damage = 1;
        int hIdx = hazardIds.IndexOf(hitId);
        if (hIdx != -1) damage = hazards[hIdx].damage;
        else
        {
            int eIdx = enemyIds.IndexOf(hitId);
            if (eIdx != -1) damage = enemyDataList[eIdx].damage;
        }

        currentHealth -= damage;
        invincibilityTimer = invincibilityDuration;

        if (currentHealth <= 0)
        {
            ResetPlayer();
            return true;
        }
        return false;
    }

    void ResetPowerupEffects()
    {
        maxJumps = 1;
        jumpsRemaining = Mathf.Min(jumpsRemaining, maxJumps);
        gravity = baseGravity;
    }

    void UpdatePlayerMatrix(int index, Vector3 pos, Quaternion rot, Vector3 scale)
    {
        Matrix4x4 newMatrix = Matrix4x4.TRS(pos, rot, scale);
        matrices[index] = newMatrix;
        playerMatrices[0] = newMatrix;

        CollisionManager.Instance.UpdateCollider(playerID, pos, Vector3.Scale(new Vector3(width, height, depth), scale));
        CollisionManager.Instance.UpdateMatrix(playerID, newMatrix);
        if (cameraFollow != null) cameraFollow.SetPlayerPosition(pos);
    }

    void UpdateEnemyMatrix(int enemyIdx, int globalIdx, Vector3 pos, Quaternion rot, Vector3 scale)
    {
        Matrix4x4 newMatrix = Matrix4x4.TRS(pos, rot, scale);
        matrices[globalIdx] = newMatrix;
        enemyMatrices[enemyIdx] = newMatrix;

        CollisionManager.Instance.UpdateCollider(enemyIds[enemyIdx], pos, Vector3.Scale(new Vector3(width, height, depth), scale));
        CollisionManager.Instance.UpdateMatrix(enemyIds[enemyIdx], newMatrix);
    }

    void CollectPowerup(int hitId)
    {
        int index = powerupIds.IndexOf(hitId);
        if (index == -1 || powerups[index].isCollected) return;

        PowerupData data = powerups[index];
        data.isCollected = true;
        powerupTimer = data.duration;

        CollisionManager.Instance.RemoveCollider(hitId);

        switch (data.type)
        {
            case PowerupType.Invincibility: invincibilityTimer = data.duration; break;
            case PowerupType.DoubleJump: maxJumps = 2; jumpsRemaining++; break;
            case PowerupType.LowGravity: gravity *= gravityReductionScale; break;
        }
    }

    public void ResetPlayer()
    {
        currentHealth = maxHealth;
        invincibilityTimer = 0;
        playerVelocity = Vector3.zero;
        isGrounded = false;
        ResetPowerupEffects();

        int index = colliderIds.IndexOf(playerID);
        if (index == -1) return;

        DecomposeMatrix(matrices[index], out _, out Quaternion rot, out Vector3 scale);
        UpdatePlayerMatrix(index, playerStartPos, rot, scale);

        Debug.Log("Player Reset");
    }

    void RenderBoxes()
    {
        Vector3 camPos = cameraFollow.transform.position;
        Vector3 camForward = cameraFollow.transform.forward;

        DrawMatrixList(boxMatrices, boxMaterial, camPos, camForward);
        DrawMatrixList(enemyMatrices, enemyMaterial, camPos, camForward);
        DrawMatrixList(playerMatrices, playerMaterial, camPos, camForward);
        DrawMatrixList(hazardMatrices, hazardMaterial, camPos, camForward);

        List<Matrix4x4> visiblePowerups = new List<Matrix4x4>();
        for (int i = 0; i < powerups.Length; i++)
            if (!powerups[i].isCollected) visiblePowerups.Add(powerupMatrices[i]);

        DrawMatrixList(visiblePowerups, powerupMaterial, camPos, camForward);
    }

    void DrawMatrixList(List<Matrix4x4> list, Material mat, Vector3 camPos, Vector3 camForward)
    {
        Matrix4x4[] matrixArray = list.ToArray();

        for (int i = 0; i < matrixArray.Length; i++)
        {
            Vector3 objectPos = matrixArray[i].GetColumn(3);
            Vector3 toObject = (objectPos - camPos).normalized;
            if (Vector3.Dot(camForward, toObject) < 0)
            {
                matrixArray[i].SetColumn(0, Vector4.zero);
                matrixArray[i].SetColumn(1, Vector4.zero);
                matrixArray[i].SetColumn(2, Vector4.zero);
            }
        }

        for (int i = 0; i < matrixArray.Length; i += 1023)
        {
            int batchSize = Mathf.Min(1023, matrixArray.Length - i);
            Matrix4x4[] batch = new Matrix4x4[batchSize];
            Array.Copy(matrixArray, i, batch, 0, batchSize);
            Graphics.DrawMeshInstanced(cubeMesh, 0, mat, batch, batchSize);
        }
    }

    int CreateBox(Vector3 position, Vector3 scale, Quaternion rotation, bool isDynamic, string type = "box")
    {
        Vector3 worldSize = new Vector3(width * scale.x, height * scale.y, depth * scale.z);
        int id = CollisionManager.Instance.RegisterCollider(position, worldSize, isDynamic);
        Matrix4x4 matrix = Matrix4x4.TRS(position, rotation, scale);

        matrices.Add(matrix);
        colliderIds.Add(id);
        CollisionManager.Instance.UpdateMatrix(id, matrix);

        switch (type)
        {
            case "player": playerMatrices.Add(matrix); break;
            case "enemy": enemyMatrices.Add(matrix); break;
            case "hazard": hazardMatrices.Add(matrix); break;
            case "powerup": powerupMatrices.Add(matrix); break;
            default: boxMatrices.Add(matrix); break;
        }
        return id;
    }

    void CreateCubeMesh()
    {
        cubeMesh = new Mesh();
        float hx = width / 2f, hy = height / 2f, hz = depth / 2f;
        cubeMesh.vertices = new Vector3[] {
            new Vector3(-hx, -hy, -hz), new Vector3(hx, -hy, -hz), new Vector3(hx, -hy, hz), new Vector3(-hx, -hy, hz),
            new Vector3(-hx, hy, -hz), new Vector3(hx, hy, -hz), new Vector3(hx, hy, hz), new Vector3(-hx, hy, hz)
        };
        cubeMesh.triangles = new int[] { 0, 4, 1, 1, 4, 5, 2, 6, 3, 3, 6, 7, 0, 3, 4, 4, 3, 7, 1, 5, 2, 2, 5, 6, 0, 1, 3, 3, 1, 2, 4, 7, 5, 5, 7, 6 };
        cubeMesh.RecalculateNormals();
    }

    void CreatePlayer() => playerID = CreateBox(playerStartPos, Vector3.one, Quaternion.identity, true, "player");
    void CreateGround() => CreateBox(new Vector3(0, groundY, constantZPosition), new Vector3(groundWidth / width, 1f / height, groundDepth / depth), Quaternion.identity, false);
    void SpawnPlatforms() { 
        foreach (var p in platforms) 
            CreateBox(
                p.position, 
                p.scale, 
                Quaternion.Euler(p.rotationEuler), 
                !p.isStatic); 
    }
    void SpawnEnemies()
    {
        for (int i = 0; i < enemies.Length; i++)
        {
            var e = enemies[i];
            Vector3 pos = e.spawnPos + new Vector3(Mathf.Lerp(e.roamXRange.x, e.roamXRange.y, (i % 10) / 9f), 0, 0);
            enemyIds.Add(CreateBox(pos, e.scale, Quaternion.identity, true, "enemy"));
            enemyDataList.Add(e);
        }
    }
    void SpawnHazards() { 
        foreach (var h in hazards) hazardIds.Add(
            CreateBox(
                h.position, 
                h.scale, 
                Quaternion.Euler(h.rotationEuler), 
                false, 
                "hazard")); 
    }
    void SpawnPowerups() { 
        foreach (var p in powerups) powerupIds.Add(
            CreateBox(
                p.position, 
                p.scale, 
                Quaternion.identity, 
                false, 
                "powerup")); 
    }

    bool CheckCollisionAt(int id, Vector3 pos) => CollisionManager.Instance.CheckCollision(id, pos, out _);
    void DecomposeMatrix(Matrix4x4 m, out Vector3 p, out Quaternion r, out Vector3 s) { p = m.GetPosition(); r = m.rotation; s = m.lossyScale; }


    private void OnDrawGizmos()
    {
        // --- PLATFORMS (Yellow) ---
        Gizmos.color = Color.yellow;
        if (platforms != null)
        {
            foreach (var p in platforms)
            {
                Vector3 size = new Vector3(width * p.scale.x, height * p.scale.y, depth * p.scale.z);
                DrawWireCube(p.position, Quaternion.Euler(p.rotationEuler), size);
            }
        }

        // --- HAZARDS (Orange) ---
        Gizmos.color = new Color(1f, 0.5f, 0f);
        if (hazards != null)
        {
            foreach (var h in hazards)
            {
                Vector3 size = new Vector3(width * h.scale.x, height * h.scale.y, depth * h.scale.z);
                DrawWireCube(h.position, Quaternion.Euler(h.rotationEuler), size);
            }
        }

        // --- POWERUPS (Magenta) ---
        Gizmos.color = Color.magenta;
        if (powerups != null)
        {
            for (int i = 0; i < powerups.Length; i++)
            {
                var p = powerups[i];

                // Hide the gizmo in Play Mode if it's already been collected
                if (Application.isPlaying && p.isCollected) continue;

                Vector3 size = new Vector3(width * p.scale.x, height * p.scale.y, depth * p.scale.z);

                if (Application.isPlaying && powerupMatrices.Count > i)
                {
                    // Draw at live position (useful if you decide to make them move/bob later)
                    DrawWireCubeFromMatrix(powerupMatrices[i]);
                }
                else
                {
                    // Draw at spawn position for editor preview
                    DrawWireCube(p.position, Quaternion.identity, size);
                }
            }
        }

        // --- ENEMIES (Red) ---
        if (enemies != null)
        {
            for (int i = 0; i < enemies.Length; i++)
            {
                var e = enemies[i];
                Vector3 size = new Vector3(width * e.scale.x, height * e.scale.y, depth * e.scale.z);

                // Draw Roam Range
                Gizmos.color = new Color(1f, 0f, 0f, 0.2f);
                Vector3 rangeCenter = e.spawnPos + new Vector3((e.roamXRange.x + e.roamXRange.y) * 0.5f, 0, 0);
                Vector3 rangeSize = new Vector3(e.roamXRange.y - e.roamXRange.x + size.x, size.y, size.z);
                Gizmos.DrawWireCube(rangeCenter, rangeSize);

                Gizmos.color = Color.red;
                if (Application.isPlaying && enemyMatrices.Count > i)
                    DrawWireCubeFromMatrix(enemyMatrices[i]);
                else
                    DrawWireCube(e.spawnPos, Quaternion.identity, size);
            }
        }

        // --- PLAYER (Cyan) ---
        Gizmos.color = Color.cyan;
        if (Application.isPlaying && playerMatrices.Count > 0)
            DrawWireCubeFromMatrix(playerMatrices[0]);
        else
            DrawWireCube(playerStartPos, Quaternion.identity, Vector3.one);

        // --- GROUND (Green) ---
        Gizmos.color = Color.green;
        Vector3 groundPos = new Vector3(0, groundY, constantZPosition);
        Vector3 groundSize = new Vector3(groundWidth, 1f, groundDepth);
        Gizmos.DrawWireCube(groundPos, groundSize);
    }

    private void DrawWireCubeFromMatrix(Matrix4x4 matrix)
    {
        Matrix4x4 oldMatrix = Gizmos.matrix;
        Gizmos.matrix = matrix;
        Gizmos.DrawWireCube(Vector3.zero, new Vector3(width, height, depth));
        Gizmos.matrix = oldMatrix;
    }

    private void DrawWireCube(Vector3 pos, Quaternion rot, Vector3 size)
    {
        Matrix4x4 m = Matrix4x4.TRS(pos, rot, Vector3.one);
        Matrix4x4 oldMatrix = Gizmos.matrix;
        Gizmos.matrix = m;
        Gizmos.DrawWireCube(Vector3.zero, size);
        Gizmos.matrix = oldMatrix;
    }
}

[Serializable] public class PlatformData { 
    public Vector3 position; 
    public Vector3 scale = Vector3.one; 
    public Vector3 rotationEuler; 
    public bool isStatic = true; 
}

[Serializable] public class EnemyData { 
    public Vector3 spawnPos; 
    public float speed = 3f; 
    public Vector2 roamXRange = new Vector2(-5f, 5f); 
    public Vector3 scale = Vector3.one; 
    public bool useGravity = true; 
    public int damage = 1; 

    [HideInInspector] public float currentDir = 1f; 
    [HideInInspector] public float verticalVelocity = 0f; 
}

[Serializable] public class HazardData { 
    public string name = "Hazard"; 
    public Vector3 position; 
    public Vector3 scale = Vector3.one; 
    public Vector3 rotationEuler; 
    public int damage = 1; 
}

public enum PowerupType { Invincibility, DoubleJump, LowGravity }
[Serializable] public class PowerupData { 
    public string name = "New Powerup"; 
    public PowerupType type; 
    public Vector3 position; 
    public float duration = 10f; 
    public Vector3 scale = new Vector3(0.5f, 0.5f, 0.5f); 
    
    [HideInInspector] public bool isCollected = false; 
}