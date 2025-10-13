using UnityEngine;
using UnityEngine.AI;
using System.Collections;

[RequireComponent(typeof(NavMeshAgent))]
public class FactoryNavMeshUpdater : MonoBehaviour
{
    private NavMeshAgent navMeshAgent;

    private void Awake()
    {
        navMeshAgent = GetComponent<NavMeshAgent>();
    }

    public void UpdateNavMesh()
    {
        // Use the dedicated runner to start the coroutine.
        // This is safe even if this GameObject becomes inactive.
        CoroutineRunner.Instance.RunCoroutine(RebakeAgentShape());
    }

    private IEnumerator RebakeAgentShape()
    {
        if (navMeshAgent == null || !navMeshAgent.isOnNavMesh)
        {
            yield break; // Exit if the agent is not properly set up
        }

        navMeshAgent.enabled = false;
        yield return null; // Wait one frame
        navMeshAgent.enabled = true;
    }
}