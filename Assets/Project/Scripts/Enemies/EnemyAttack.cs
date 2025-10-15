using UnityEngine;
using System.Collections;
using AutoForge.Player;
using AutoForge.Factory;

namespace AutoForge.Enemies
{
    [RequireComponent(typeof(EnemyMovement))]
    [RequireComponent(typeof(Rigidbody))]
    public class EnemyAttack : MonoBehaviour
    {
        [Header("Attack Settings")]
        [SerializeField] private float attackDamage = 10f;
        [SerializeField] private float attackRate = 1.5f;
        [SerializeField] public float attackRange = 2f;
        [SerializeField] private float playerKnockbackForce = 150f; // How hard the player is pushed back

        [Header("Attack Visuals")]
        [SerializeField] private GameObject attackEffectPrefab;
        [SerializeField] private float attackLungeForce = 200f;
        [SerializeField] private float attackRecoilForce = 100f;

        private EnemyMovement _enemyMovement;
        private Rigidbody _rb;
        private float _attackCooldown = 0f;
        private bool _isAttacking = false;

        private void Awake()
        {
            _enemyMovement = GetComponent<EnemyMovement>();
            _rb = GetComponent<Rigidbody>();
            _rb.isKinematic = true;
            _rb.useGravity = false;
        }

        private void Update()
        {
            if (_attackCooldown > 0)
            {
                _attackCooldown -= Time.deltaTime;
            }

            if (_enemyMovement.target != null && _attackCooldown <= 0 && !_isAttacking)
            {
                TryAttack();
            }
        }

        private void TryAttack()
        {
            float distanceToTarget = Vector3.Distance(transform.position, _enemyMovement.target.position);
            if (distanceToTarget <= attackRange)
            {
                StartCoroutine(PerformAttackVisuals());
            }
        }

        private IEnumerator PerformAttackVisuals()
        {
            _isAttacking = true;
            _enemyMovement.agent.enabled = false;
            _rb.isKinematic = false;
            _rb.constraints = RigidbodyConstraints.FreezeRotation; // Allow movement but not weird tumbling

            // --- FIX: FLATTEN LUNGE DIRECTION ---
            // We set the 'y' component to 0 to ensure the lunge is horizontal.
            Vector3 directionToTarget = (_enemyMovement.target.position - transform.position).normalized;
            directionToTarget.y = 0;
            _rb.AddForce(directionToTarget * attackLungeForce, ForceMode.Impulse);

            yield return new WaitForSeconds(0.1f);

            // --- DAMAGE, VFX, and KNOCKBACK ---
            if (_enemyMovement.target.TryGetComponent<PlayerStats>(out PlayerStats playerStats))
            {
                playerStats.TakeDamage(attackDamage);
                // --- NEW: APPLY KNOCKBACK TO PLAYER ---
                playerStats.ApplyKnockback(directionToTarget, playerKnockbackForce);
            }
            else if (_enemyMovement.target.TryGetComponent<PowerCore>(out PowerCore powerCore))
            {
                powerCore.TakeDamage(attackDamage);
            }

            if (attackEffectPrefab != null)
            {
                Instantiate(attackEffectPrefab, _enemyMovement.target.position, Quaternion.identity);
            }

            _rb.AddForce(-directionToTarget * attackRecoilForce, ForceMode.Impulse);

            yield return new WaitForSeconds(0.3f);

            // --- FIX: RESET CONSTRAINTS AND POSITION ---
            // Ensure the enemy doesn't get stuck in the air.
            _rb.linearVelocity = Vector3.zero;
            _rb.isKinematic = true;
            _rb.constraints = RigidbodyConstraints.FreezeAll; // Fully freeze physics again

            _enemyMovement.agent.enabled = true;
            // This helps sync the agent's position with the rigidbody's final position.
            _enemyMovement.agent.Warp(transform.position);
            _attackCooldown = attackRate;
            _isAttacking = false;
        }
    }
}

