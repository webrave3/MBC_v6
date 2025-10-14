using UnityEngine;
using UnityEngine.AI;
using AutoForge.Player;
using AutoForge.Factory;

namespace AutoForge.Enemies
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class EnemyMovement : MonoBehaviour
    {
        [Header("AI Settings")]
        [Tooltip("The chance (0-100) that this enemy will target the Power Core instead of the Player.")]
        [Range(0, 100)] public int coreTargetingChance = 75;
        [Tooltip("How often the enemy re-evaluates its target (in seconds).")]
        [SerializeField] private float targetUpdateRate = 1.0f;
        [Tooltip("How close the player must be to be considered a potential target.")]
        [SerializeField] private float playerDetectionRadius = 20f;

        [Header("Movement")]
        [SerializeField] private float moveSpeed = 3.5f;

        private NavMeshAgent agent;
        private EnemyAttack _enemyAttack; // Reference to the attack script
        public Transform target { get; private set; }

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            _enemyAttack = GetComponent<EnemyAttack>(); // Get the attack script
            agent.speed = moveSpeed;

            // --- IMPORTANT ---
            // Set the agent's stopping distance based on the attack range
            if (_enemyAttack != null)
            {
                agent.stoppingDistance = _enemyAttack.attackRange;
            }
        }

        private void Start()
        {
            StartCoroutine(UpdateTargetCoroutine());
        }

        private System.Collections.IEnumerator UpdateTargetCoroutine()
        {
            while (true)
            {
                DecideTarget();
                yield return new WaitForSeconds(targetUpdateRate);
            }
        }

        private void DecideTarget()
        {
            Transform playerTarget = null;
            Transform coreTarget = PowerCore.Instance?.transform;

            // Check if player is within detection radius
            if (PlayerStats.Instance != null)
            {
                float distanceToPlayer = Vector3.Distance(transform.position, PlayerStats.Instance.transform.position);
                if (distanceToPlayer <= playerDetectionRadius)
                {
                    playerTarget = PlayerStats.Instance.transform;
                }
            }

            if (playerTarget == null && coreTarget == null) target = null;
            else if (playerTarget != null && coreTarget == null) target = playerTarget;
            else if (playerTarget == null && coreTarget != null) target = coreTarget;
            else
            {
                int roll = Random.Range(0, 100);
                target = (roll < coreTargetingChance) ? coreTarget : playerTarget;
            }
        }

        private void Update()
        {
            if (target != null && agent.isActiveAndEnabled)
            {
                // Constantly update the destination for a moving target
                agent.SetDestination(target.position);
            }
        }
    }
}

