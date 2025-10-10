using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace AutoForge
{
    public class PlayerShoot : MonoBehaviour
    {
        [Header("Required References")]
        public GameObject bulletPrefab;
        public Renderer muzzleFlash;
        public Camera playerCamera;

        [Header("Spawn Point Offsets (from camera)")]
        [Tooltip("How far forward the bullet spawns.")]
        public float forwardOffset = 1.5f;
        [Tooltip("How far to the right the bullet spawns.")]
        public float rightOffset = 0.3f;
        [Tooltip("How far up the bullet spawns.")]
        public float upOffset = -0.2f;


        void Start()
        {
            if (muzzleFlash != null)
                muzzleFlash.enabled = false;
        }

        public void OnAttack(InputValue value)
        {
            if (value.isPressed)
            {
                Shoot();
            }
        }

        void Shoot()
        {
            if (bulletPrefab == null || playerCamera == null) return;

            // --- THE FOOLPROOF SPAWN LOGIC ---

            // 1. Get the camera's current position and rotation.
            Transform camTransform = playerCamera.transform;

            // 2. Calculate the spawn position by starting at the camera and moving out.
            Vector3 spawnPosition = camTransform.position +
                                    (camTransform.forward * forwardOffset) +
                                    (camTransform.right * rightOffset) +
                                    (camTransform.up * upOffset);

            // 3. The bullet's rotation should always match the camera's rotation.
            Quaternion spawnRotation = camTransform.rotation;

            // 4. Instantiate the bullet at the calculated position.
            Instantiate(bulletPrefab, spawnPosition, spawnRotation);

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

