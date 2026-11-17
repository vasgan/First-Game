using System;
using FirstGame;
using UnityEditor;
using UnityEngine;

namespace FirstGameEditor
{
    public static class CombatReadabilityValidation
    {
        [MenuItem("First Game/Validate Combat Readability")]
        public static void Validate()
        {
            ValidateTower("BaseTower");
            ValidateTower("RapidTower");
            ValidateTower("CannonTower");
            ValidateTower("BeamTower");

            Require(TowerRangeView.DisplayedDiameter(4.1f) == 8.2f,
                "Attack range must display its exact diameter.");
            Require(TowerRangeView.DisplayedDiameter(2.4f) == 4.8f,
                "Effect range must display its exact diameter.");
            Require(TowerRangeView.DisplayedDiameter(0f) == 0f,
                "A tower without an area effect must not display an effect radius.");

            Debug.Log("FIRST_GAME_COMBAT_READABILITY_VALIDATION_PASSED");
        }

        private static void ValidateTower(string assetName)
        {
            TowerConfig config = AssetDatabase.LoadAssetAtPath<TowerConfig>($"Assets/Game/Data/{assetName}.asset");
            Require(config && config.levels != null && config.levels.Length > 0,
                $"{assetName} must provide readable level data.");

            string title = GameUI.FormatTowerTitle(config, config.levels.Length);
            Require(title.Contains(config.displayName) && title.Contains($"level {config.levels.Length}"),
                $"{assetName} must expose its selected level in the action title.");
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException("Combat readability validation failed: " + message);
        }
    }
}
