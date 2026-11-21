using System.Collections.Generic;
using UnityEngine;

namespace FirstGame
{
    public enum GameLanguage { English, Russian }

    /// <summary>Small local table for the prototype UI. The selected language is intentionally device-local.</summary>
    public static class Localization
    {
        private const string LanguageKey = "firstgame.language";
        public static GameLanguage Current { get; private set; }
        public static bool IsRussian => Current == GameLanguage.Russian;

        public static void Load() => Current = (GameLanguage)Mathf.Clamp(PlayerPrefs.GetInt(LanguageKey, 0), 0, 1);
        public static void Toggle()
        {
            Load();
            Current = IsRussian ? GameLanguage.English : GameLanguage.Russian;
            PlayerPrefs.SetInt(LanguageKey, (int)Current);
            PlayerPrefs.Save();
        }
        public static void SetLanguage(GameLanguage language)
        {
            Current = language;
            PlayerPrefs.SetInt(LanguageKey, (int)Current);
            PlayerPrefs.Save();
        }

        public static string LanguageLabel => IsRussian ? "РУССКИЙ" : "ENGLISH";
        public static string T(string english, string russian) => IsRussian ? russian : english;
        public static string TowerName(TowerConfig config) => config ? TranslateTowerText(config.displayName) : T("Tower", "Башня");
        public static string TowerDescription(TowerConfig config) => config ? TranslateTowerText(config.description) : string.Empty;
        public static string SpecializationName(TowerSpecializationConfig specialization) => specialization != null ? TranslateTowerText(specialization.displayName) : string.Empty;
        public static string SpecializationDescription(TowerSpecializationConfig specialization) => specialization != null ? TranslateTowerText(specialization.description) : string.Empty;

        private static string TranslateTowerText(string value)
        {
            if (!IsRussian || string.IsNullOrEmpty(value)) return value;
            var translations = new Dictionary<string, string>
            {
                ["Base Tower"] = "Базовая башня", ["Ice Tower"] = "Ледяная башня",
                ["fireTower"] = "Огненная башня", ["ElectroTower"] = "Электро-башня",
                ["Single projectiles"] = "Одиночные снаряды",
                ["Slows one enemy by 30% for 1.25s"] = "Замедляет врага на 30% на 1,25 с",
                ["Burst damage to all enemies in range"] = "Взрывной урон всем врагам в радиусе",
                ["Instant beam hits one enemy"] = "Мгновенный луч поражает одного врага"
                , ["Longshot"] = "Дальнобой"
                , ["Volley"] = "Залп"
                , ["High damage and long range for armored targets."] = "Высокий урон и дальность против бронированных целей."
                , ["Fast fire for dense groups near the route."] = "Быстрый огонь против плотных групп у маршрута."
            };
            return translations.TryGetValue(value, out string translated) ? translated : value;
        }
    }
}
