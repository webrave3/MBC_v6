using System; // Required for using Actions
using UnityEngine;

namespace AutoForge.Core
{
    /// <summary>
    /// The central nervous system of the game. A static class that holds all game events.
    /// Allows different systems to communicate without direct references (decoupling).
    /// </summary>
    public static class EventManager
    {
        // This event is an "Action" that takes one parameter: a Vector3 for the death position.
        // Any script can subscribe to this event to be notified when an enemy dies.
        public static event Action<Vector3> OnEnemyDied;

        // This is how other scripts will trigger the event.
        public static void RaiseEnemyDied(Vector3 position)
        {
            // The ?.Invoke() is a safe way to call the event, doing nothing if no one is listening.
            OnEnemyDied?.Invoke(position);
        }
    }
}
