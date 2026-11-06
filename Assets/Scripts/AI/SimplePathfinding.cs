using UnityEngine;
using System.Collections.Generic;

public class SimplePathfinding : MonoBehaviour
{
    [Header("Settings")]
    public float moveSpeed = 4f;
    public float turnSpeed = 10f;
    public float obstacleCheckDistance = 2f;
    public LayerMask obstacleLayer;
    
    [Header("State")]
    public Vector3 targetPosition;
    public bool hasTarget = false;
    public bool isMoving = false;
    
    private Vector3 currentVelocity;
    
    public void SetTarget(Vector3 position)
    {
        targetPosition = position;
        hasTarget = true;
    }
    
    public void ClearTarget()
    {
        hasTarget = false;
        currentVelocity = Vector3.zero;
    }
    
    private void Update()
    {
        if (!hasTarget) return;

        // Town <-> open field travel goes through the gate, so a companion does
        // not press into the north wall when the player runs out into the field.
        Vector3 aim = TownLayout.RouteViaGate(transform.position, targetPosition);

        Vector3 direction = (aim - transform.position).normalized;
        direction.y = 0;

        if (direction.magnitude < 0.1f) return;

        // Check for obstacles
        if (Physics.Raycast(transform.position + Vector3.up * 0.5f, direction, obstacleCheckDistance, obstacleLayer))
        {
            // Try to go around
            direction = FindAlternatePath(direction);
        }

        // Move
        Vector3 before = transform.position;
        Vector3 step = direction * moveSpeed * Time.deltaTime;
        Vector3 wanted = before + step;

        // This mover writes transform.position directly, so it never collides
        // with the boundary walls. Without this clamp a survivor fleeing a witch
        // can walk straight out of the map (seen at x = -87 during testing).
        // Same playable area the witches use: town square plus the north field.
        Vector3 allowed = TownLayout.ClampToPlayable(wanted, 3f);

        // If the clamp ate the move the agent is pressed against a boundary.
        // Standing there is what pinned a fleeing survivor in the map corner
        // until a witch caught him, so deflect along the wall instead.
        if ((allowed - wanted).sqrMagnitude > 0.0004f)
            allowed = SlideAlongBoundary(before, direction, step.magnitude);

        transform.position = allowed;

        // Rotate. Face where we actually went, so a sliding agent does not
        // moonwalk sideways into the wall.
        Vector3 moved = allowed - before;
        Vector3 facing = moved.sqrMagnitude > 0.000001f ? moved.normalized : direction;
        if (facing != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(facing);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
        }

        // Check if reached target
        if (Vector3.Distance(transform.position, targetPosition) < 0.5f)
        {
            hasTarget = false;
            currentVelocity = Vector3.zero;
        }
    }

    /// <summary>
    /// Finds a direction close to <paramref name="direction"/> that still makes
    /// progress inside the playable area. Deflections start small so a normal
    /// graze of the boundary keeps the agent on course, and widen until the
    /// move actually happens - which also lets a cornered agent walk out along
    /// the wall instead of freezing in the corner.
    /// </summary>
    private Vector3 SlideAlongBoundary(Vector3 from, Vector3 direction, float distance)
    {
        if (distance <= 0.00001f) return from;

        float minProgress = distance * 0.5f;

        for (int i = 1; i <= 7; i++)
        {
            float angle = i * 22.5f;
            for (int s = -1; s <= 1; s += 2)
            {
                Vector3 dir = Quaternion.Euler(0f, angle * s, 0f) * direction;
                Vector3 candidate = TownLayout.ClampToPlayable(from + dir * distance, 3f);
                if ((candidate - from).magnitude >= minProgress)
                    return candidate;
            }
        }

        return TownLayout.ClampToPlayable(from + direction * distance, 3f);
    }

    private Vector3 FindAlternatePath(Vector3 blockedDirection)
    {
        Vector3 origin = transform.position + Vector3.up * 0.5f;

        // Progressively wider whiskers: a 45 degree sidestep gets round a
        // building corner, an 80 degree one gets round a long wall.
        float[] angles = { -45f, 45f, -80f, 80f, -110f, 110f };

        for (int i = 0; i < angles.Length; i++)
        {
            Vector3 dir = Quaternion.Euler(0f, angles[i], 0f) * blockedDirection;
            if (!Physics.Raycast(origin, dir, obstacleCheckDistance, obstacleLayer))
                return dir;
        }

        return blockedDirection;
    }
    
    public float GetDistanceToTarget()
    {
        return hasTarget ? Vector3.Distance(transform.position, targetPosition) : 0f;
    }
    
    public bool HasReachedTarget()
    {
        return hasTarget && Vector3.Distance(transform.position, targetPosition) < 0.5f;
    }
}
