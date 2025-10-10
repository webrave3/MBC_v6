using UnityEngine;
using AutoForge.Player; // To access PlayerStats

namespace AutoForge.Core
{
    public class Building : MonoBehaviour
    {
        [Header("Effect")]
        [SerializeField] private float damageBonus = 5f;

        private void Start()
        {
            // When this building is created, apply its bonus to the player
            if (PlayerStats.Instance != null)
            {
                PlayerStats.Instance.AddDamageBoost(damageBonus);
            }
        }
    }
}
