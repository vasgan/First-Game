using FirstGame;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace FirstGameEditor
{
    /// <summary>Creates level data assets. Adding a map means adding data here, not changing scene generation.</summary>
    public static class LevelContentBuilder
    {
        private const string DataRoot = "Assets/Game/Data";
        private const string ResourcesRoot = "Assets/Game/Resources";
        private const string TileRoot = "Assets/Game/Art/Levels";
        private const string SourceTileRoot = "art_done/levels";

        [MenuItem("First Game/Create or Update Level Content")]
        public static void CreateOrUpdateAssets()
        {
            if (!AssetDatabase.IsValidFolder(ResourcesRoot))
                AssetDatabase.CreateFolder("Assets/Game", "Resources");
            EnsureRouteSprites();

            WaveConfig levelOneWaves = AssetDatabase.LoadAssetAtPath<WaveConfig>(DataRoot + "/WaveConfig.asset");
            if (!levelOneWaves) throw new System.Exception("Level 1 wave data is missing.");
            ApplyDirectWaveStats(levelOneWaves, new[] { 80, 90, 100, 110, 120, 130, 145, 160, 180, 200 }, 1.5f);

            WaveConfig levelTwoWaves = LoadOrCreate<WaveConfig>(DataRoot + "/Level02WaveConfig.asset");
            levelTwoWaves.enemyCount = 10;
            levelTwoWaves.spawnInterval = .72f;
            levelTwoWaves.completionReward = 25;
            levelTwoWaves.waves = CreateSecondMapWaves();

            LevelDefinition levelOne = LoadOrCreate<LevelDefinition>(DataRoot + "/Level01.asset");
            levelOne.levelIndex = 1; levelOne.sceneName = "Level01"; levelOne.displayName = "LEVEL 1";
            levelOne.waveConfig = levelOneWaves;
            levelOne.routePalette = LoadRoutePalette();
            levelOne.route = new[]
            {
                new Vector2(-3.2f, -9f), new Vector2(-3.2f, -5f), new Vector2(0f, -5f), new Vector2(0f, -.5f),
                new Vector2(3.1f, -.5f), new Vector2(3.1f, 4.3f), new Vector2(.2f, 4.3f), new Vector2(.2f, 8.8f)
            };
            levelOne.buildSpots = new[] { new Vector2(-1.65f, -6.5f), new Vector2(1.6f, -3.25f), new Vector2(1.5f, 1.35f), new Vector2(-1.6f, 6.25f) };

            LevelDefinition levelTwo = LoadOrCreate<LevelDefinition>(DataRoot + "/Level02.asset");
            levelTwo.levelIndex = 2; levelTwo.sceneName = "Level01"; levelTwo.displayName = "LEVEL 2";
            levelTwo.waveConfig = levelTwoWaves;
            levelTwo.routePalette = LoadRoutePalette();
            // This reversed S route makes the central build locations less useful than on level one.
            levelTwo.route = new[]
            {
                new Vector2(3.2f, -9f), new Vector2(3.2f, -5.8f), new Vector2(-2.5f, -5.8f), new Vector2(-2.5f, -1.4f),
                new Vector2(2.4f, -1.4f), new Vector2(2.4f, 2.7f), new Vector2(-1.8f, 2.7f), new Vector2(-1.8f, 8.8f)
            };
            levelTwo.buildSpots = new[] { new Vector2(1.1f, -7.0f), new Vector2(-.2f, -3.6f), new Vector2(.2f, .5f), new Vector2(-3.6f, 4.8f) };

            LevelCatalog catalog = LoadOrCreate<LevelCatalog>(ResourcesRoot + "/LevelCatalog.asset");
            catalog.levels = new[] { levelOne, levelTwo };
            EditorUtility.SetDirty(levelOneWaves); EditorUtility.SetDirty(levelTwoWaves);
            EditorUtility.SetDirty(levelOne); EditorUtility.SetDirty(levelTwo); EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("FIRST_GAME_LEVEL_CONTENT_COMPLETE");
        }

        private static WaveDefinition[] CreateSecondMapWaves()
        {
            int[] counts = { 7, 9, 11, 12, 14, 15, 17, 19, 21, 24 };
            int[] health = { 90, 105, 120, 135, 150, 165, 185, 205, 225, 250 };
            WaveDefinition[] waves = new WaveDefinition[counts.Length];
            for (int index = 0; index < waves.Length; index++)
            {
                waves[index] = new WaveDefinition
                {
                    enemyCount = counts[index], maxHealth = health[index], moveSpeed = 1.65f,
                    spawnInterval = Mathf.Max(.42f, .82f - index * .04f), completionReward = 18 + index * 3,
                    healthMultiplier = 1f
                };
            }
            return waves;
        }

        private static void ApplyDirectWaveStats(WaveConfig config, int[] health, float speed)
        {
            if (config.waves == null) return;
            for (int index = 0; index < config.waves.Length && index < health.Length; index++)
            {
                config.waves[index].maxHealth = health[index];
                config.waves[index].moveSpeed = speed;
                config.waves[index].healthMultiplier = 1f;
            }
        }

        private static RouteTilePalette LoadRoutePalette()
        {
            return new RouteTilePalette
            {
                roadCenterHorizontal = LoadTile("road_center_horizontal.png"),
                roadCenterVertical = LoadTile("road_center_vertical.png"),
                roadBorderTopHorizontal = LoadTile("road_border_top_horizontal.png"),
                roadBorderBottomHorizontal = LoadTile("road_border_bottom_horizontal.png"),
                roadBorderLeftVertical = LoadTile("road_border_left_vertical.png"),
                roadBorderRightVertical = LoadTile("road_border_right_vertical.png"),
                roadBorderInnerCornerTopLeft = LoadTile("road_border_inner_corner_top_left.png"),
                roadBorderInnerCornerTopRight = LoadTile("road_border_inner_corner_top_right.png"),
                roadBorderInnerCornerBottomLeft = LoadTile("road_border_inner_corner_bottom_left.png"),
                roadBorderInnerCornerBottomRight = LoadTile("road_border_inner_corner_bottom_right.png"),
                roadBorderOuterCornerTopLeft = LoadTile("road_border_outer_corner_top_left.png"),
                roadBorderOuterCornerTopRight = LoadTile("road_border_outer_corner_top_right.png"),
                roadBorderOuterCornerBottomLeft = LoadTile("road_border_outer_corner_bottom_left.png"),
                roadBorderOuterCornerBottomRight = LoadTile("road_border_outer_corner_bottom_right.png")
            };
        }

        private static Sprite LoadTile(string name)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(TileRoot + "/" + name);
            if (!sprite) throw new System.Exception("Missing level tile: " + name);
            return sprite;
        }

        private static void EnsureRouteSprites()
        {
            CopySource("horizontal_top_line.png", "road_border_top_horizontal.png");
            CopySource("horizontal_bottom_line.png", "road_border_bottom_horizontal.png");
            CopySource("vertical_left_line.png", "road_border_left_vertical.png");
            CopySource("road_border_inner_corner_top_left.png", "road_border_inner_corner_top_left.png");
            CopySource("road_border_inner_corner_top_right.png", "road_border_inner_corner_top_right.png");
            CopySource("road_border_inner_corner_bottom_left.png", "road_border_inner_corner_bottom_left.png");
            CopySource("road_border_inner_corner_bottom_right.png", "road_border_inner_corner_bottom_right.png");
            CopySource("road_border_outer_corner_top_left.png", "road_border_outer_corner_top_left.png");
            CopySource("road_border_outer_corner_top_right.png", "road_border_outer_corner_top_right.png");
            CopySource("road_border_outer_corner_bottom_left.png", "road_border_outer_corner_bottom_left.png");
            CreateTransparentTile("road_center_horizontal.png");
            CreateTransparentTile("road_center_vertical.png");
            CreateMirroredTile("vertical_left_line.png", "road_border_right_vertical.png");
            CreateMirroredTile("road_border_outer_corner_bottom_left.png", "road_border_outer_corner_bottom_right.png");
            AssetDatabase.Refresh();

            foreach (string path in Directory.GetFiles(TileRoot, "road_*.png"))
                ConfigureTileImport(path.Replace('\\', '/'));
        }

        private static void CopySource(string sourceName, string targetName)
        {
            string target = TileRoot + "/" + targetName;
            if (!File.Exists(target)) FileUtil.CopyFileOrDirectory(SourceTileRoot + "/" + sourceName, target);
        }

        private static void CreateTransparentTile(string targetName)
        {
            string target = TileRoot + "/" + targetName;
            if (File.Exists(target)) return;
            Texture2D texture = new(100, 100, TextureFormat.RGBA32, false);
            File.WriteAllBytes(target, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
        }

        private static void CreateMirroredTile(string sourceName, string targetName)
        {
            string target = TileRoot + "/" + targetName;
            if (File.Exists(target)) return;
            Texture2D texture = new(2, 2, TextureFormat.RGBA32, false);
            if (!texture.LoadImage(File.ReadAllBytes(SourceTileRoot + "/" + sourceName)))
                throw new System.Exception("Could not read source tile: " + sourceName);

            for (int y = 0; y < texture.height; y++)
                for (int x = 0; x < texture.width / 2; x++)
                {
                    Color left = texture.GetPixel(x, y);
                    int otherX = texture.width - 1 - x;
                    texture.SetPixel(x, y, texture.GetPixel(otherX, y));
                    texture.SetPixel(otherX, y, left);
                }
            File.WriteAllBytes(target, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
        }

        private static void ConfigureTileImport(string path)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (!importer) return;
            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = 100;
            importer.spritePivot = new Vector2(.5f, .5f);
            importer.filterMode = FilterMode.Bilinear;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
        }

        private static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset) return asset;
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }
    }
}
