using UnityEngine;
using UnityEngine.AI;

public class NavMeshDataHolder : MonoBehaviour
{
    public NavMeshData navMeshData;
    
    private void Awake()
    {
        if (navMeshData != null)
        {
            NavMesh.AddNavMeshData(navMeshData);
        }
    }
}
