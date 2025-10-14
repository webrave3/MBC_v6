using UnityEngine;
using AutoForge.Player; // To find PlayerStats
using AutoForge.Factory; // To find PowerCore

namespace AutoForge.Enemies
{
    [RequireComponent(typeof(EnemyMovement))]
    public class EnemyAttack : MonoBehaviour
    {
        [Header("Attack Settings")]
        [SerializeField] private float attackDamage = 10f;
        [SerializeField] private float attackRate = 1.5f; // How many seconds between each attack

        // --- FIX IS HERE ---
        // This variable is now public, so EnemyMovement can read it.
        [Tooltip("How close the enemy needs to be to attack. This MUST match the NavMeshAgent's Stopping Distance.")]
        [SerializeField] public float attackRange = 2f;

        private EnemyMovement _enemyMovement;
        private float _attackCooldown = 0f;

        private void Awake()
        {
            _enemyMovement = GetComponent<EnemyMovement>();
        }

        private void Update()
        {
            if (_attackCooldown > 0)
            {
                _attackCooldown -= Time.deltaTime;
            }

            if (_enemyMovement.target != null && _attackCooldown <= 0)
            {
                TryAttack();
            }
        }

        private void TryAttack()
        {
            float distanceToTarget = Vector3.Distance(transform.position, _enemyMovement.target.position);

            if (distanceToTarget <= attackRange)
            {
                if (_enemyMovement.target.TryGetComponent<PlayerStats>(out PlayerStats playerStats))
                {
                    playerStats.TakeDamage(attackDamage);
                    Debug.Log($"Attacked Player for {attackDamage} damage!");
                }
                else if (_enemyMovement.target.TryGetComponent<PowerCore>(out PowerCore powerCore))
                {
                    powerCore.TakeDamage(attackDamage);
                    Debug.Log($"Attacked Core for {attackDamage} damage!");
                }

                _attackCooldown = attackRate;
            }
        }
    }
}

