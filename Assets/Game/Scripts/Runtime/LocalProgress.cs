using UnityEngine;

namespace FirstGame
{
    /// <summary>Small, versioned local save for the campaign map selection.</summary>
    public static class LocalProgress
    {
        private const int CurrentVersion = 1;
        private const string VersionKey = "firstgame.progress.version";
        private const string CompletedLevelKey = "firstgame.progress.completedLevel";
        private static int LevelCount => Mathf.Max(1, LevelSelection.Catalog && LevelSelection.Catalog.levels != null
            ? LevelSelection.Catalog.levels.Length : 1);

        public static int CompletedLevel { get; private set; }

        public static void Load()
        {
            int version = PlayerPrefs.GetInt(VersionKey, 0);
            int completed = PlayerPrefs.GetInt(CompletedLevelKey, 0);
            if (!TryNormalize(version, completed, out int normalized))
            {
                Reset();
                return;
            }

            CompletedLevel = normalized;
        }

        public static bool IsUnlocked(int levelIndex)
        {
            Load();
            return levelIndex >= 1 && levelIndex <= LevelCount && levelIndex <= CompletedLevel + 1;
        }

        public static void MarkCompleted(int levelIndex)
        {
            Load();
            CompletedLevel = Mathf.Clamp(Mathf.Max(CompletedLevel, levelIndex), 0, LevelCount);
            PlayerPrefs.SetInt(VersionKey, CurrentVersion);
            PlayerPrefs.SetInt(CompletedLevelKey, CompletedLevel);
            PlayerPrefs.Save();
        }

        public static string GetLevelStatus(int levelIndex) => levelIndex <= CompletedLevel
            ? Localization.T("COMPLETE", "ПРОЙДЕН")
            : IsUnlocked(levelIndex) ? Localization.T("AVAILABLE", "ДОСТУПЕН") : Localization.T("LOCKED", "ЗАБЛОКИРОВАН");

        public static bool TryNormalize(int version, int completedLevel, out int normalizedCompletedLevel)
        {
            normalizedCompletedLevel = 0;
            if (version != CurrentVersion || completedLevel < 0 || completedLevel > LevelCount)
                return false;

            normalizedCompletedLevel = completedLevel;
            return true;
        }

        private static void Reset()
        {
            CompletedLevel = 0;
            PlayerPrefs.SetInt(VersionKey, CurrentVersion);
            PlayerPrefs.SetInt(CompletedLevelKey, CompletedLevel);
            PlayerPrefs.Save();
        }
    }
}
