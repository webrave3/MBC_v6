using UnityEngine;

namespace AutoForge.Core
{
    /// <summary>
    /// A simple utility script that destroys the GameObject it is attached to after a set delay.
    /// </summary>
    public class SelfDestruct : MonoBehaviour
    {
        public float delay = 1f;

        void Start()
        {
            // Destroy this GameObject after 'delay' seconds.
            Destroy(gameObject, delay);
        }
    }
}
