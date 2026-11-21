using System;
using System.Reflection;
using FirstGame;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace FirstGameEditor
{
    public static class GameUiValidation
    {
        [MenuItem("First Game/Validate Match UI")]
        public static void Validate()
        {
            ValidateBootstrapTextConstruction();
            Localization.SetLanguage(GameLanguage.English);
            TowerConfig config = AssetDatabase.LoadAssetAtPath<TowerConfig>("Assets/Game/Data/RapidTower.asset");
            Require(config && config.levels.Length > 1, "Rapid tower configuration must provide two levels.");

            string card = GameUI.FormatTowerCard(config, 0, true);
            Require(card.Contains("Cost") && card.Contains("Rate") && card.Contains("Range"),
                "Tower card must show cost, rate, and range.");
            Require(card.Contains("Need"), "Tower card must explain insufficient gold.");

            string upgrade = GameUI.FormatUpgradeAction(config, 0, 0);
            Require(upgrade.Contains("Upgrade") && upgrade.Contains("Damage") && upgrade.Contains("Need"),
                "Upgrade presentation must compare levels and explain insufficient gold.");

            TowerConfig baseTower = AssetDatabase.LoadAssetAtPath<TowerConfig>("Assets/Game/Data/BaseTower.asset");
            Require(baseTower && baseTower.specializations != null && baseTower.specializations.Length == 2,
                "Base tower must offer exactly two specializations.");
            string specialization = GameUI.FormatSpecializationAction(baseTower.specializations[0], 0);
            Require(specialization.Contains("Longshot") && specialization.Contains("Cost") && specialization.Contains("Need"),
                "A specialization must name its effect, price, and affordability before purchase.");

            Require(GameUI.FormatNormalWaveStatus(GameState.Wave, 4, 10) == "Wave 4/10",
                "Normal HUD must show current and final wave.");
            Require(GameUI.FormatNormalPreparation(4, 10) == "Next: wave 5/10",
                "Normal HUD must show the next wave during preparation.");
            WaveSettings previewSettings = new(5, null, null, 90, 1.25f, .5f, 20,
                new[]
                {
                    new WaveGroupSettings(2, null, null, 90, 1.25f, .5f),
                    new WaveGroupSettings(3, null, null, 140, 2f, .5f)
                });
            string preview = GameUI.FormatWavePreview(3, 10, previewSettings);
            Require(preview.Contains("Next: wave 3/10") && preview.Contains("Enemies: 5") &&
                    preview.Contains("2 × Max HP: 90") && preview.Contains("3 × Max HP: 140") &&
                    preview.Contains("Base speed: 2") && preview.Contains("START WAVE"),
                "Wave preview must show the actual next-wave number, total, group health, speed, and actions.");

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
            Require(GameController.IsTestProfileAvailable,
                "Test Profile must remain available in the Editor for explicit test builds.");
            Require(!GameController.IsTestProfileAvailableInPlayer(false) &&
                    GameController.IsTestProfileAvailableInPlayer(true),
                "Only development players may expose Test Profile.");
            Require(GameUI.FormatSellAction(42).Contains("42"),
                "Tower action card must expose the sell refund before confirmation.");
            Require(GameUI.FormatTargetingAction(TargetingRule.Strongest).Contains("Strongest"),
                "Tower action card must show the active targeting rule.");
            string victory = GameUI.FormatMatchResult(true, 10, 10, 3, 5);
            Require(victory.Contains("VICTORY") && victory.Contains("10/10") && victory.Contains("3/5"),
                "Victory result must show the cleared wave and final base health.");
            string defeat = GameUI.FormatMatchResult(false, 4, 10, 0, 5);
            Require(defeat.Contains("DEFEAT") && defeat.Contains("wave 4/10") &&
                    defeat.Contains("enemies reached the base") && defeat.Contains("0/5"),
                "Defeat result must explain the loss, wave reached, and final base health.");

            LocalProgress.Load();
            Require(LocalProgress.IsUnlocked(1), "The first level must remain available after loading local progress.");
            Require(!LocalProgress.TryNormalize(0, 0, out _),
                "An empty save must be rejected for recovery.");
            Require(!LocalProgress.TryNormalize(1, 99, out _),
                "An incompatible progress value must be rejected for recovery.");
            Require(LocalProgress.TryNormalize(1, 0, out int completed) && completed == 0,
                "A valid empty campaign progress must remain playable.");

            Localization.SetLanguage(GameLanguage.Russian);
            Require(GameUI.FormatPauseLabel(false) == "ПАУЗА" && GameUI.GetTutorialText(0).Contains("светящуюся"),
                "Russian UI must expose Cyrillic pause and tutorial strings.");
            Require(GameUI.FormatTowerCard(config, 0, true).Contains("Нужно ещё"),
                "Russian tower card must preserve long affordability guidance.");
            Require(GameUI.FormatMatchResult(false, 4, 10, 0, 5).Contains("Причина: враги добрались до базы"),
                "Russian defeat result must retain the cause of the loss.");
            Require(GameUI.FormatWavePreview(3, 10, previewSettings).Contains("Далее: волна 3/10") &&
                    GameUI.FormatWavePreview(3, 10, previewSettings).Contains("Действия:"),
                "Russian wave preview must keep the next-wave information and available actions.");
            Localization.SetLanguage(GameLanguage.English);

            SafeAreaLayout.GetAnchors(new Rect(0f, 60f, 1080f, 1800f), new Vector2(1080f, 1920f),
                out Vector2 anchorMin, out Vector2 anchorMax);
            Require(anchorMin == new Vector2(0f, .03125f) && anchorMax == new Vector2(1f, .96875f),
                "Safe area must map pixels to normalized canvas anchors.");

            Debug.Log("FIRST_GAME_MATCH_UI_VALIDATION_PASSED");
        }

        private static void ValidateBootstrapTextConstruction()
        {
            GameObject parent = new("BootstrapTextValidation", typeof(RectTransform));
            try
            {
                MethodInfo createText = typeof(BootstrapLoader).GetMethod("CreateText", BindingFlags.NonPublic | BindingFlags.Static);
                Require(createText != null, "Bootstrap text factory must exist.");
                Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                Text text = (Text)createText.Invoke(null, new object[]
                {
                    "ValidationText", parent.transform, font, "VALIDATION", 24, Vector2.zero, Vector2.one
                });
                Require(text && text.font == font && text.GetComponents<Graphic>().Length == 1,
                    "Bootstrap text must be created as one valid UI graphic without an Image conflict.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(parent);
            }
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new Exception("Match UI validation failed: " + message);
        }
    }
}
