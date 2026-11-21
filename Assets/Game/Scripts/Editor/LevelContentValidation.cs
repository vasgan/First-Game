using System;
using FirstGame;
using UnityEditor;
using UnityEngine;

namespace FirstGameEditor
{
    public static class LevelContentValidation
    {
        [MenuItem("First Game/Validate Level Content")]
        public static void Validate()
        {
            LevelCatalog catalog = AssetDatabase.LoadAssetAtPath<LevelCatalog>("Assets/Game/Resources/LevelCatalog.asset");
            Require(catalog && catalog.levels != null && catalog.levels.Length == 2, "Catalog must expose exactly two levels.");
            for (int index = 0; index < catalog.levels.Length; index++)
            {
                LevelDefinition level = catalog.levels[index];
                Require(level && level.levelIndex == index + 1, "Level index must match catalog order.");
                Require(level.route != null && level.route.Length >= 2, "Level requires a route from data.");
                Require(level.buildSpots != null && level.buildSpots.Length >= 3, "Level requires build spots from data.");
                Require(HasCompleteRoutePalette(level.routePalette), "Level requires the complete route-tile palette.");
                Require(level.waveConfig && level.waveConfig.NormalWaveCount == 10, "Level requires ten waves from data.");
                foreach (WaveDefinition wave in level.waveConfig.waves)
                    Require(wave.enemyCount > 0 && wave.maxHealth > 0 && wave.moveSpeed > 0f,
                        "Each level wave must define count, base health, and base speed.");
            }
            Require(catalog.levels[0].route[0] != catalog.levels[1].route[0] &&
                catalog.levels[0].buildSpots[0] != catalog.levels[1].buildSpots[0],
                "Second map must change the route and build placement.");
            Debug.Log("FIRST_GAME_LEVEL_CONTENT_VALIDATION_PASSED");
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new Exception("Level content validation failed: " + message);
        }

        private static bool HasCompleteRoutePalette(RouteTilePalette palette)
        {
            return palette != null && palette.roadCenterHorizontal && palette.roadCenterVertical &&
                   palette.roadBorderTopHorizontal && palette.roadBorderBottomHorizontal &&
                   palette.roadBorderLeftVertical && palette.roadBorderRightVertical &&
                   palette.roadBorderInnerCornerTopLeft && palette.roadBorderInnerCornerTopRight &&
                   palette.roadBorderInnerCornerBottomLeft && palette.roadBorderInnerCornerBottomRight &&
                   palette.roadBorderOuterCornerTopLeft && palette.roadBorderOuterCornerTopRight &&
                   palette.roadBorderOuterCornerBottomLeft && palette.roadBorderOuterCornerBottomRight;
        }
    }
}
