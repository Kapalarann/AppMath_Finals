using UnityEngine;

public class PlayerCameraFollow : MonoBehaviour
{
    // Target to follow (will be set by EnhancedMeshGenerator)
    private Transform _target;
    
    // Camera positioning
    public Vector3 offset = new Vector3(0, -5, -10);
    public float smoothSpeed = 0.125f;
    
    // Optional camera constraints
    public bool useConstraints = true;
    public Vector2 xConstraint = new Vector2(-100f, 100f);
    public Vector2 yConstraint = new Vector2(-50f, 100f);
    
    // Camera options
    public bool followX = true;
    public bool followY = true;
    public bool followZ = false;
    
    // Reference to player position
    private Vector3 playerPosition;
    
    // This method will be called by EnhancedMeshGenerator to set the player position
    public void SetPlayerPosition(Vector3 position)
    {
        playerPosition = position;
    }
    
    void LateUpdate()
    {
        if (playerPosition == Vector3.zero)
            return;
            
        // Calculate desired position based on player position and offset
        Vector3 desiredPosition = transform.position;
        
        // Only follow axes we want to follow
        if (followX) desiredPosition.x = playerPosition.x + offset.x;
        if (followY) desiredPosition.y = playerPosition.y + offset.y;
        if (followZ) desiredPosition.z = playerPosition.z + offset.z;
        
        // Apply constraints if enabled
        if (useConstraints)
        {
            desiredPosition.x = Mathf.Clamp(desiredPosition.x, xConstraint.x, xConstraint.y);
            desiredPosition.y = Mathf.Clamp(desiredPosition.y, yConstraint.x, yConstraint.y);
        }
        
        // Smoothly interpolate between current position and desired position
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);
        transform.position = smoothedPosition;
        
        // Look at player (optional - comment this out if you want fixed camera angle)
        // transform.LookAt(playerPosition);
    }
}
