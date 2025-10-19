// /Assets/Project/Scripts/Player/PlayerWorldLoader.cs
using UnityEngine;
using AutoForge.World;

[RequireComponent(typeof(CharacterController))] // Or Rigidbody
public class PlayerWorldLoader : MonoBehaviour
{
    private CharacterController _controller;
    // private Rigidbody _rb; // Use if you have Rigidbody instead

    void Awake()
    {
        _controller = GetComponent<CharacterController>();
        if (_controller != null)
        {
            _controller.enabled = false;
            Debug.Log("<color=orange>[PlayerLoader]</color> Player CharacterController DISABLED on Awake, awaiting world gen...");
        }

        /* // Rigidbody version
        _rb = GetComponent<Rigidbody>();
        if (_rb != null)
        {
            _rb.isKinematic = true; // Stop gravity temporarily
            Debug.Log("<color=orange>[PlayerLoader]</color> Player Rigidbody set to Kinematic on Awake, awaiting world gen...");
        }
        */

        if (_controller == null /* && _rb == null */)
        {
            Debug.LogError("<color=red><b>[PlayerLoader ERROR]</b></color> No CharacterController (or Rigidbody) found on player!", this);
        }
    }

    void Start()
    {
        if (WorldManager.Instance != null)
        {
            WorldManager.Instance.OnInitialWorldGenerated += EnablePlayerPhysics;
        }
        else
        {
            Debug.LogError("<color=red>[PlayerLoader ERROR]</color> Could not find WorldManager.Instance in Start! Enabling physics immediately as fallback.");
            EnablePlayerPhysics(); // Failsafe: Enable immediately if WorldManager is missing
        }
    }

    private void OnDestroy()
    {
        // Cleanup subscription
        if (WorldManager.Instance != null)
        {
            WorldManager.Instance.OnInitialWorldGenerated -= EnablePlayerPhysics;
        }
    }

    private void EnablePlayerPhysics()
    {
        // --- This is called by WorldManager *after* initial chunks are generated ---

        // Unsubscribe immediately to prevent multiple calls if event fires again somehow
        if (WorldManager.Instance != null)
        {
            WorldManager.Instance.OnInitialWorldGenerated -= EnablePlayerPhysics;
        }

        Vector3 currentPosition = transform.position; // Store current position

        if (_controller != null)
        {
            // --- FIX FOR FLOATING/FALLING ---
            // Ensure controller is disabled before teleporting
            _controller.enabled = false;

            // Move controller slightly up BEFORE enabling it the first time
            Vector3 slightUp = currentPosition + Vector3.up * 0.5f; // Increased bump height
            transform.position = slightUp;
            Debug.Log($"<color=lightblue>[PlayerLoader]</color> Teleported player slightly up to {slightUp}");

            // Now enable the controller at the new position
            _controller.enabled = true;
            // --- END FIX ---

            // Check if grounded immediately after enabling
            bool isGrounded = _controller.isGrounded;
            Debug.Log($"<color=green>[PlayerLoader]</color> CharacterController ENABLED. Initial isGrounded: {isGrounded}");

            // If still not grounded, something else is wrong (gravity, layers, etc.)
            if (!isGrounded && Time.time < 2f)
            { // Only warn early on
                Debug.LogWarning($"<color=yellow>[PlayerLoader Warning]</color> Player is not grounded immediately after enabling controller. Check gravity/layers/mesh.");
            }
        }

        /* // Rigidbody version
        if (_rb != null)
        {
            // Teleport slightly up first
            transform.position = currentPosition + Vector3.up * 0.5f; // Increased bump
             Debug.Log($"<color=lightblue>[PlayerLoader]</color> Teleported player slightly up to {transform.position}");

            // Then allow physics to take over
            _rb.isKinematic = false;
            Debug.Log("<color=green>[PlayerLoader]</color> Rigidbody physics ENABLED (isKinematic = false).");
        }
        */
    }
}