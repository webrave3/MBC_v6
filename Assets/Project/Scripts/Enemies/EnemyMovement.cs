using UnityEngine;
using UnityEngine.AI;
using AutoForge.Player;
using AutoForge.Factory;

namespace AutoForge.Enemies
{
    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(EnemyAttack))]
    public class EnemyMovement : MonoBehaviour
    {
        [Header("AI Behavior")]
        [Tooltip("On spawn, what is the chance (0-100) this enemy will immediately target the player?")]
        [Range(0, 100)] public int initialPlayerTargetChance = 10;

        [Tooltip("When hit by the player, what is the chance (0-100) this enemy will get angry and attack the player back?")]
        [Range(0, 100)] public int aggroChanceOnHit = 80;

        [Tooltip("If angry at the player, how far away does the player need to be for this enemy to give up and return to the core?")]
        public float playerDeaggroDistance = 25f;

        [Header("Movement")]
        [SerializeField] private float moveSpeed = 3.5f;

        public NavMeshAgent agent { get; private set; }
        private EnemyAttack _enemyAttack;
        public Transform target { get; private set; }

        private bool _isAggroedOnPlayer = false;

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            _enemyAttack = GetComponent<EnemyAttack>();
            agent.speed = moveSpeed;

            if (_enemyAttack != null)
            {
                agent.stoppingDistance = _enemyAttack.attackRange;
            }
        }

        private void Start()
        {
            DecideInitialTarget();
        }

        public void OnTookDamageFromPlayer()
        {
            if (_isAggroedOnPlayer) return;

            int roll = Random.Range(0, 100);
            if (roll < aggroChanceOnHit)
            {
                _isAggroedOnPlayer = true;
                if (PlayerStats.Instance != null)
                {
                    target = PlayerStats.Instance.transform;
                }
            }
        }

        private void DecideInitialTarget()
        {
            int roll = Random.Range(0, 100);
            if (roll < initialPlayerTargetChance && PlayerStats.Instance != null)
            {
                // This enemy will start by targeting the player
                _isAggroedOnPlayer = true;
                target = PlayerStats.Instance.transform;
            }
            else
            {
                // This enemy will start by targeting the core (default behavior)
                _isAggroedOnPlayer = false;
                if (PowerCore.Instance != null)
                {
                    target = PowerCore.Instance.transform;
                }
            }
        }

        private void Update()
        {
            if (_isAggroedOnPlayer)
            {
                if (PlayerStats.Instance == null)
                {
                    _isAggroedOnPlayer = false;
                    target = PowerCore.Instance?.transform; // Revert to core if player is gone
                }
                else
                {
                    float distanceToPlayer = Vector3.Distance(transform.position, PlayerStats.Instance.transform.position);
                    if (distanceToPlayer > playerDeaggroDistance)
                    {
                        _isAggroedOnPlayer = false;
                        target = PowerCore.Instance?.transform; // Revert to core
                    }
                }
            }

            if (target != null && agent.isActiveAndEnabled)
            {
                agent.SetDestination(target.position);
            }
        }
    }
}

