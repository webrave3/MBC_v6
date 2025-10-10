using UnityEngine;

namespace AutoForge.Enemies
{
    // This attribute allows us to right-click in the Project window and create a new "Enemy Data" file.
    [CreateAssetMenu(fileName = "NewEnemyData", menuName = "AutoForge/Enemy Data")]
    public class EnemyData : ScriptableObject
    {
        [Header("Stats")]
        public float maxHealth = 100f;
        public float moveSpeed = 3.5f;

        [Header("Cosmetics")]
        public string enemyName = "Grunt";
        // We could add references to models, materials, sounds, etc. here later.
    }
}
