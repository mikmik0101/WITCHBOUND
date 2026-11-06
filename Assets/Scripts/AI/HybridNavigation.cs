using UnityEngine;
using UnityEngine.AI;

public class HybridNavigation : MonoBehaviour
{
    [Header("Settings")]
    public float moveSpeed = 4f;
    public float turnSpeed = 10f;
    public float obstacleCheckDistance = 2f;
    public LayerMask obstacleLayer;
    public float angularSpeed = 120f;
    public float acceleration = 8f;
    public float stoppingDistance = 0.5f;
    
    [Header("State")]
    public bool hasTarget = false;
    public Vector3 targetPosition;
    public bool isMoving = false;
    
    [Header("Navigation Mode")]
    public bool useNavMesh = true;
    public bool navMeshAvailable = false;
    
    private NavMeshAgent navAgent;
    private SimplePathfinding simplePathfinding;
    private bool initialized = false;
    
    // NavMeshAgent-compatible properties
    public float speed
    {
        get
        {
            if (useNavMesh && navMeshAvailable && navAgent != null)
                return navAgent.speed;
            if (simplePathfinding != null)
                return simplePathfinding.moveSpeed;
            return moveSpeed;
        }
        set
        {
            moveSpeed = value;
            if (useNavMesh && navMeshAvailable && navAgent != null)
                navAgent.speed = value;
            if (simplePathfinding != null)
                simplePathfinding.moveSpeed = value;
        }
    }
    
    public float remainingDistance
    {
        get
        {
            if (useNavMesh && navMeshAvailable && navAgent != null)
                return navAgent.remainingDistance;
            if (simplePathfinding != null && simplePathfinding.hasTarget)
                return simplePathfinding.GetDistanceToTarget();
            return 0f;
        }
    }
    
    public bool pathPending
    {
        get
        {
            if (useNavMesh && navMeshAvailable && navAgent != null)
                return navAgent.pathPending;
            return false;
        }
    }
    
    public bool isStopped
    {
        get
        {
            if (useNavMesh && navMeshAvailable && navAgent != null)
                return navAgent.isStopped;
            if (simplePathfinding != null)
                return !simplePathfinding.hasTarget;
            return true;
        }
        set
        {
            if (useNavMesh && navMeshAvailable && navAgent != null)
                navAgent.isStopped = value;
            if (simplePathfinding != null)
            {
                if (value)
                    simplePathfinding.ClearTarget();
            }
        }
    }
    
    public Vector3 velocity
    {
        get
        {
            if (useNavMesh && navMeshAvailable && navAgent != null)
                return navAgent.velocity;
            if (simplePathfinding != null && simplePathfinding.hasTarget)
            {
                return (simplePathfinding.targetPosition - transform.position).normalized * simplePathfinding.moveSpeed;
            }
            return Vector3.zero;
        }
    }
    
    private void Awake()
    {
        navAgent = GetComponent<NavMeshAgent>();
        simplePathfinding = GetComponent<SimplePathfinding>();
        
        if (simplePathfinding == null)
            simplePathfinding = gameObject.AddComponent<SimplePathfinding>();
        
        simplePathfinding.moveSpeed = moveSpeed;
        simplePathfinding.turnSpeed = turnSpeed;
        simplePathfinding.obstacleCheckDistance = obstacleCheckDistance;
        simplePathfinding.obstacleLayer = obstacleLayer;
        
        navMeshAvailable = CheckNavMeshAvailability();
        
        if (!navMeshAvailable)
        {
            useNavMesh = false;
            Debug.Log($"[HybridNavigation] NavMesh not available, using SimplePathfinding fallback on {gameObject.name}");
        }
        else
        {
            Debug.Log($"[HybridNavigation] NavMesh available, using NavMeshAgent on {gameObject.name}");
        }
        
        initialized = true;
    }
    
    private bool CheckNavMeshAvailability()
    {
        NavMeshHit hit;
        if (NavMesh.SamplePosition(transform.position, out hit, 10f, NavMesh.AllAreas))
        {
            return true;
        }
        
        NavMeshTriangulation triangulation = NavMesh.CalculateTriangulation();
        if (triangulation.vertices.Length > 0)
        {
            return true;
        }
        
        return false;
    }
    
    public void SetDestination(Vector3 position)
    {
        targetPosition = position;
        hasTarget = true;
        isMoving = true;
        
        if (useNavMesh && navMeshAvailable && navAgent != null)
        {
            navAgent.isStopped = false;
            navAgent.SetDestination(position);
        }
        else
        {
            if (simplePathfinding != null)
            {
                simplePathfinding.SetTarget(position);
            }
        }
    }
    
    public void ResetPath()
    {
        hasTarget = false;
        isMoving = false;
        
        if (useNavMesh && navMeshAvailable && navAgent != null)
        {
            navAgent.ResetPath();
        }
        else
        {
            if (simplePathfinding != null)
            {
                simplePathfinding.ClearTarget();
            }
        }
    }
    
    public void Stop()
    {
        isStopped = true;
    }
    
    public void Resume()
    {
        isStopped = false;
    }
    
    public bool HasReachedTarget()
    {
        if (useNavMesh && navMeshAvailable && navAgent != null)
        {
            if (!navAgent.pathPending && navAgent.remainingDistance <= navAgent.stoppingDistance + 0.1f)
            {
                return true;
            }
            return false;
        }
        else
        {
            if (simplePathfinding != null)
            {
                return simplePathfinding.HasReachedTarget();
            }
            return false;
        }
    }
    
    public string GetNavigationMode()
    {
        if (useNavMesh && navMeshAvailable)
            return "NavMesh";
        else
            return "SimplePathfinding";
    }
}
