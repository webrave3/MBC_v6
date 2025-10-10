using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

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
            // Get a reference to the builder script on this same object
            playerBuilder = GetComponent<PlayerBuilder>();
        }

        void Start()
        {
            if (muzzleFlash != null)
                muzzleFlash.enabled = false;
        }

        // Called by the "Attack" Input Action (Left Mouse) from Send Messages
        public void OnAttack(InputValue value)
        {
            // CRITICAL CHECK: If the builder exists AND is in build mode, do nothing.
            if (playerBuilder != null && playerBuilder.IsInBuildMode)
            {
                return; // Exit the function immediately
            }

            // If we are not in build mode, then fire the weapon.
            if (value.isPressed)
            {
                Shoot();
            }
        }

        void Shoot()
        {
            if (bulletPrefab == null || playerCamera == null) return;

            // Calculate spawn position based on camera and offsets
            Vector3 spawnPosition = playerCamera.transform.position +
                                    (playerCamera.transform.forward * forwardOffset) +
                                    (playerCamera.transform.right * rightOffset) +
                                    (playerCamera.transform.up * upOffset);

            // Spawn the bullet with the camera's rotation so it aims correctly
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

