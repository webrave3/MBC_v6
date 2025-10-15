using UnityEngine;

/// <summary>
/// A simple script that destroys its own GameObject after a set lifetime.
/// Attach this to particle effect prefabs to ensure they are cleaned up from the scene.
/// </summary>
public class AutoDestroyParticle : MonoBehaviour
{
    [Tooltip("The time in seconds after which the GameObject will be destroyed.")]
    [SerializeField]
    private float lifetime = 0.25f;

    private void Start()
    {
        // Tell Unity to destroy this GameObject after 'lifetime' seconds.
        Destroy(gameObject, lifetime);
    }
}
