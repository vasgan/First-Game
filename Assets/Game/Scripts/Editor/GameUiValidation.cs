using System;
using FirstGame;
using UnityEditor;
using UnityEngine;

namespace FirstGameEditor
{
    public static class GameUiValidation
    {
        [MenuItem("First Game/Validate Match UI")]
        public static void Validate()
        {
            TowerConfig config = AssetDatabase.LoadAssetAtPath<TowerConfig>("Assets/Game/Data/RapidTower.asset");
            Require(config && config.levels.Length > 1, "Rapid tower configuration must provide two levels.");

            string card = GameUI.FormatTowerCard(config, 0, true);
            Require(card.Contains("Cost") && card.Contains("Rate") && card.Contains("Range"),
                "Tower card must show cost, rate, and range.");
            Require(card.Contains("Need"), "Tower card must explain insufficient gold.");

            string upgrade = GameUI.FormatUpgradeAction(config, 0, 0);
            Require(upgrade.Contains("Upgrade") && upgrade.Contains("Damage") && upgrade.Contains("Need"),
                "Upgrade presentation must compare levels and explain insufficient gold.");

            Require(GameUI.FormatNormalWaveStatus(GameState.Wave, 4, 10) == "Wave 4/10",
                "Normal HUD must show current and final wave.");
            Require(GameUI.FormatNormalPreparation(4, 10) == "Next: wave 5/10",
                "Normal HUD must show the next wave during preparation.");

            Require(GameUI.GetTutorialText(0).Contains("build spot"),
                "Tutorial must first direct a player to a build spot.");
            Require(GameUI.GetTutorialText(1).Contains("Choose a tower"),
                "Tutorial must explain buying a tower.");
            Require(GameUI.GetTutorialText(2).Contains("START WAVE"),
                "Tutorial must explain starting a wave.");
            Require(GameUI.GetTutorialText(3).Contains("Base value"),
                "Tutorial must explain base-health loss.");

            Require(GameUI.FormatPauseLabel(false) == "PAUSE" && GameUI.FormatPauseLabel(true) == "RESUME",
                "Pause control must expose pause and resume labels.");
            Require(GameUI.FormatSpeedLabel(1f) == "SPEED ×1" && GameUI.FormatSpeedLabel(2f) == "SPEED ×2",
                "Speed control must expose both supported speeds.");
            Require(GameUI.FormatVolumeLabel("MUSIC", .65f) == "MUSIC: 65%" &&
                GameUI.FormatVolumeLabel("EFFECTS", 0f) == "EFFECTS: 0%",
                "Audio settings must expose persisted channel volumes as percentages.");
            Require(GameUI.FormatSellAction(42).Contains("42"),
                "Tower action card must expose the sell refund before confirmation.");
            Require(GameUI.FormatTargetingAction(TargetingRule.Strongest).Contains("Strongest"),
                "Tower action card must show the active targeting rule.");

            LocalProgress.Load();
            Require(LocalProgress.IsUnlocked(1), "The first level must remain available after loading local progress.");
            Require(!LocalProgress.TryNormalize(0, 0, out _),
                "An empty save must be rejected for recovery.");
            Require(!LocalProgress.TryNormalize(1, 99, out _),
                "An incompatible progress value must be rejected for recovery.");
            Require(LocalProgress.TryNormalize(1, 0, out int completed) && completed == 0,
                "A valid empty campaign progress must remain playable.");

            SafeAreaLayout.GetAnchors(new Rect(0f, 60f, 1080f, 1800f), new Vector2(1080f, 1920f),
                out Vector2 anchorMin, out Vector2 anchorMax);
            Require(anchorMin == new Vector2(0f, .03125f) && anchorMax == new Vector2(1f, .96875f),
                "Safe area must map pixels to normalized canvas anchors.");

            Debug.Log("FIRST_GAME_MATCH_UI_VALIDATION_PASSED");
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new Exception("Match UI validation failed: " + message);
        }
    }
}
