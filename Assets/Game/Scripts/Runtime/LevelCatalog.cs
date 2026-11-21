using UnityEngine;

namespace FirstGame
{
    [CreateAssetMenu(menuName = "First Game/Level Catalog")]
    public sealed class LevelCatalog : ScriptableObject
    {
        public LevelDefinition[] levels = System.Array.Empty<LevelDefinition>();

        public LevelDefinition Get(int levelIndex)
        {
            if (levels == null) return null;
            foreach (LevelDefinition level in levels)
                if (level && level.levelIndex == levelIndex) return level;
            return null;
        }
    }
}
