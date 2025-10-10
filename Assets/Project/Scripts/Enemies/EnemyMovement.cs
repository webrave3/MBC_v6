using UnityEngine;
using UnityEngine.AI;
using AutoForge.Enemies;

// Now requires a Rigidbody as well
[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Enemy))]
[RequireComponent(typeof(Rigidbody))]
public class EnemyMovement : MonoBehaviour
{
    private Transform playerTarget;
    private NavMeshAgent agent;
    private Enemy enemy;
    private Rigidbody rb; // Reference to the Rigidbody

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        enemy = GetComponent<Enemy>();
        rb = GetComponent<Rigidbody>(); // Get the Rigidbody component

        // --- CRUCIAL STEP ---
        // Tell the NavMeshAgent that it will NOT control the object's position directly.
        // It will only be used for path calculations.
        agent.updatePosition = false;
        agent.updateRotation = false;

        if (enemy.enemyData != null)
        {
            agent.speed = enemy.enemyData.moveSpeed;
        }

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            playerTarget = playerObject.transform;
        }
    }

    void Update()
    {
        if (playerTarget != null)
        {
            // Tell the agent where we want to go
            agent.SetDestination(playerTarget.position);
        }

        // We manually move the Rigidbody based on the agent's calculated path
        MoveEnemy();
    }

    private void MoveEnemy()
    {
        // Get the direction the agent wants to go in
        Vector3 desiredVelocity = agent.desiredVelocity;

        // Apply that direction to our Rigidbody's velocity
        // We set the Y velocity to our current Y velocity to not interfere with gravity
        rb.linearVelocity = new Vector3(desiredVelocity.x, rb.linearVelocity.y, desiredVelocity.z);

        // Make the enemy look in the direction it's moving
        if (desiredVelocity.sqrMagnitude > 0.1f) // Only rotate if we are actually moving
        {
            transform.rotation = Quaternion.LookRotation(desiredVelocity);
        }

        // This is important: we need to manually sync the agent's position with our Rigidbody's
        // so its path calculations don't fall behind.
        agent.nextPosition = rb.position;
    }
}

