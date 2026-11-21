using UnityEngine;

namespace FirstGame
{
    public static class LevelSelection
    {
        private const string CatalogResourcePath = "LevelCatalog";
        private static int selectedLevelIndex = 1;

        public static LevelCatalog Catalog => Resources.Load<LevelCatalog>(CatalogResourcePath);
        public static int SelectedLevelIndex => selectedLevelIndex;
        public static LevelDefinition SelectedLevel => Catalog ? Catalog.Get(selectedLevelIndex) : null;

        public static bool Select(int levelIndex)
        {
            LevelDefinition level = Catalog ? Catalog.Get(levelIndex) : null;
            if (!level || !LocalProgress.IsUnlocked(levelIndex)) return false;
            selectedLevelIndex = levelIndex;
            return true;
        }
    }
}
