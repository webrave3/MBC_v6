using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using AutoForge.Core;
using AutoForge.Player;

namespace AutoForge.Player
{
    [RequireComponent(typeof(PlayerBuilder))]
    public class PlayerShoot : MonoBehaviour
    {
        [Header("Required References")]
        [SerializeField] private GameObject bulletPrefab;
        [SerializeField] private Renderer muzzleFlash;
        [SerializeField] private Camera playerCamera;

        [Header("Spawn Position Offsets")]
        [Tooltip("How far forward from the camera the bullet spawns.")]
        [SerializeField] private float forwardOffset = 1.5f;
        [Tooltip("How far to the right of center the bullet spawns.")]
        [SerializeField] private float rightOffset = 0.3f;
        [Tooltip("How far up from the center the bullet spawns.")]
        [SerializeField] private float upOffset = -0.2f;

        private PlayerBuilder playerBuilder;

        void Awake()
        {
            playerBuilder = GetComponent<PlayerBuilder>();
        }

        void Start()
        {
            if (muzzleFlash != null)
                muzzleFlash.enabled = false;
        }

        public void OnAttack(InputValue value)
        {
            if (PlayerBuilder.Instance != null && PlayerBuilder.Instance.IsBuildMode)
            {
                return;
            }
            

            if (GameManager.Instance != null && GameManager.Instance.IsPlayerInUIMode) return;
            if (Time.timeScale == 0f) return;

            if (playerBuilder != null && playerBuilder.IsBuildMode) return;

            if (value.isPressed)
            {
                Shoot();
            }
        }

        void Shoot()
        {
            if (bulletPrefab == null || playerCamera == null) return;

            Vector3 spawnPosition = playerCamera.transform.position +
                                    (playerCamera.transform.forward * forwardOffset) +
                                    (playerCamera.transform.right * rightOffset) +
                                    (playerCamera.transform.up * upOffset);

            Instantiate(bulletPrefab, spawnPosition, playerCamera.transform.rotation);

            StartCoroutine(ShowMuzzleFlash());
        }

        private IEnumerator ShowMuzzleFlash()
        {
            if (muzzleFlash == null) yield break;

            muzzleFlash.enabled = true;
            yield return new WaitForSeconds(0.05f);
            muzzleFlash.enabled = false;
        }
    }
}