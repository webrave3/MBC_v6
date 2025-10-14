using UnityEngine;
using System;
using System.Collections;
using AutoForge.Factory;

namespace AutoForge.Player
{
    public class PlayerStats : MonoBehaviour
    {
        public static PlayerStats Instance { get; private set; }

        [Header("Offense Stats")]
        [SerializeField] private float baseDamage = 25f;
        private float damageBoost = 0f;
        public float TotalDamage => baseDamage + damageBoost;

        [Header("Defense Stats (Shields)")]
        [SerializeField] private float maxShield = 100f;
        [SerializeField] private float shieldRechargeRate = 20f; // Points per second
        [SerializeField] private float shieldRechargeDelay = 3.0f; // Seconds after taking damage before recharge starts
        public float CurrentShield { get; private set; }
        private Coroutine _rechargeCoroutine;

        // Events for UI
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
        }

        private void Start()
        {
            // Announce initial shield state for UI
            OnShieldChanged?.Invoke(CurrentShield, maxShield);
        }

        public void TakeDamage(float amount)
        {
            // Stop any ongoing shield recharge
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

            // If shields broke, pass remaining damage to the Power Core
            if (damageToCore > 0 && PowerCore.Instance != null)
            {
                PowerCore.Instance.TakeDamage(damageToCore);
            }

            // Start the recharge timer
            _rechargeCoroutine = StartCoroutine(RechargeShield());
        }

        private IEnumerator RechargeShield()
        {
            yield return new WaitForSeconds(shieldRechargeDelay);

            while (CurrentShield < maxShield)
            {
                CurrentShield += shieldRechargeRate * Time.deltaTime;
                CurrentShield = Mathf.Min(CurrentShield, maxShield); // Don't overcharge
                OnShieldChanged?.Invoke(CurrentShield, maxShield);
                yield return null; // Wait for the next frame
            }
        }

        public void AddDamageBoost(float amount)
        {
            damageBoost += amount;
            OnStatsChanged?.Invoke(this);
        }
    }
}

