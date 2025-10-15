using UnityEngine;
using System;
using System.Collections;
using AutoForge.Factory;

namespace AutoForge.Player
{
    [RequireComponent(typeof(CharacterController))] // Ensure we always have a CharacterController
    public class PlayerStats : MonoBehaviour
    {
        public static PlayerStats Instance { get; private set; }

        [Header("Offense Stats")]
        [SerializeField] private float baseDamage = 25f;
        private float damageBoost = 0f;
        public float TotalDamage => baseDamage + damageBoost;

        [Header("Defense Stats (Shields)")]
        [SerializeField] private float maxShield = 100f;
        [SerializeField] private float shieldRechargeRate = 20f;
        [SerializeField] private float shieldRechargeDelay = 3.0f;
        public float CurrentShield { get; private set; }
        private Coroutine _rechargeCoroutine;

        // --- NEW: REFERENCE TO CHARACTER CONTROLLER ---
        private CharacterController _controller;
        private Vector3 _knockbackVelocity;
        [SerializeField] private float knockbackDampening = 5f;


        public static event Action<PlayerStats> OnStatsChanged;
        public static event Action<float, float> OnShieldChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
            }
            else
            {
                Instance = this;
            }
            CurrentShield = maxShield;
            _controller = GetComponent<CharacterController>(); // Get the controller
        }

        private void Start()
        {
            OnShieldChanged?.Invoke(CurrentShield, maxShield);
        }

        // --- NEW: UPDATE METHOD TO APPLY KNOCKBACK OVER TIME ---
        private void Update()
        {
            // If there's knockback velocity, apply it and reduce it over time.
            if (_knockbackVelocity.magnitude > 0.1f)
            {
                _controller.Move(_knockbackVelocity * Time.deltaTime);
                _knockbackVelocity = Vector3.Lerp(_knockbackVelocity, Vector3.zero, knockbackDampening * Time.deltaTime);
            }
        }

        public void TakeDamage(float amount)
        {
            if (_rechargeCoroutine != null)
            {
                StopCoroutine(_rechargeCoroutine);
                _rechargeCoroutine = null;
            }

            float damageToCore = 0;
            if (amount > CurrentShield)
            {
                damageToCore = amount - CurrentShield;
                CurrentShield = 0;
            }
            else
            {
                CurrentShield -= amount;
            }

            OnShieldChanged?.Invoke(CurrentShield, maxShield);

            if (damageToCore > 0 && PowerCore.Instance != null)
            {
                PowerCore.Instance.TakeDamage(damageToCore);
            }

            _rechargeCoroutine = StartCoroutine(RechargeShield());
        }

        // --- NEW: PUBLIC METHOD FOR ENEMIES TO CALL ---
        public void ApplyKnockback(Vector3 direction, float force)
        {
            // Add the new knockback force to any existing velocity.
            _knockbackVelocity += direction.normalized * force;
        }

        private IEnumerator RechargeShield()
        {
            yield return new WaitForSeconds(shieldRechargeDelay);

            while (CurrentShield < maxShield)
            {
                CurrentShield += shieldRechargeRate * Time.deltaTime;
                CurrentShield = Mathf.Min(CurrentShield, maxShield);
                OnShieldChanged?.Invoke(CurrentShield, maxShield);
                yield return null;
            }
        }

        public void AddDamageBoost(float amount)
        {
            damageBoost += amount;
            OnStatsChanged?.Invoke(this);
        }
    }
}

