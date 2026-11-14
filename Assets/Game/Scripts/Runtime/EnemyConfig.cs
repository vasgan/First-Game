using UnityEngine;

namespace FirstGame
{
    [CreateAssetMenu(menuName = "First Game/Enemy Config")]
    public sealed class EnemyConfig : ScriptableObject
    {
        public int maxHealth = 100;
        public float moveSpeed = 1.5f;
        public int reward = 10;
    }
}
