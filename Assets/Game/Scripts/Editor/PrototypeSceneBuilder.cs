using System.Collections.Generic;
using System.IO;
using FirstGame;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace FirstGameEditor
{
    public static class PrototypeSceneBuilder
    {
        private const string Root = "Assets/Game";
        private static readonly Color Purple = new(0.68f, 0.40f, 0.92f, 1f);
        private static readonly Color DeepPurple = new(0.12f, 0.025f, 0.27f, 1f);
        private static readonly Color Panel = new(0.10f, 0.035f, 0.20f, 0.94f);
        private static Font font;

        [MenuItem("First Game/Regenerate Prototype...")]
        public static void BuildFromMenu()
        {
            if (EditorUtility.DisplayDialog("Regenerate Prototype",
                    "This overwrites prototype scenes, prefabs, balance data, and project settings.",
                    "Regenerate", "Cancel"))
                BuildPrototype();
        }

        public static void BuildPrototype()
        {
            ConfigureProject();
            ConfigureSprites();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            TowerSpineImporter.ImportAssets();
            CreateGameAssets(out EnemyConfig enemyConfig, out WaveConfig waveConfig,
                out TowerCatalog towerCatalog, out GameObject enemyPrefab);
            BuildLevel(enemyConfig, waveConfig, towerCatalog, enemyPrefab);
            BuildBootstrap();
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(Root + "/Scenes/Bootstrap.unity", true),
                new EditorBuildSettingsScene(Root + "/Scenes/Level01.unity", true)
            };
            AssetDatabase.SaveAssets();
            Debug.Log("FIRST_GAME_BUILD_COMPLETE");
        }

        [MenuItem("First Game/Build WebGL")]
        public static void BuildWebGL()
        {
            WebGlBuildValidation.Validate();

            var scenes = new List<string>();
            foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
            {
                if (scene.enabled)
                    scenes.Add(scene.path);
            }

            if (scenes.Count == 0)
                throw new BuildFailedException("Enable at least one scene in Build Profiles before building WebGL.");

            BuildPlayerOptions options = new()
            {
                scenes = scenes.ToArray(),
                locationPathName = "Builds/WebGL",
                target = BuildTarget.WebGL,
                options = BuildOptions.None
            };
            UnityEditor.Build.Reporting.BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
                throw new System.Exception("WebGL build failed: " + report.summary.result);
            Debug.Log("FIRST_GAME_WEBGL_BUILD_COMPLETE");
        }

        private static void ConfigureProject()
        {
            PlayerSettings.productName = "Glow Path Defense";
            PlayerSettings.companyName = "First Game";
            PlayerSettings.bundleVersion = "0.1.1";
            PlayerSettings.defaultScreenWidth = 1080;
            PlayerSettings.defaultScreenHeight = 1920;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.firstgame.glowpathdefense");
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, "com.firstgame.glowpathdefense");
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.iOS, ScriptingImplementation.IL2CPP);
            PlayerSettings.colorSpace = ColorSpace.Linear;
        }

        private static void ConfigureSprites()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { Root + "/Art" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (AssetImporter.GetAtPath(path) is not TextureImporter importer) continue;
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 100f;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.filterMode = FilterMode.Bilinear;
                importer.textureCompression = TextureImporterCompression.CompressedHQ;
                importer.SaveAndReimport();
            }
        }

        private static void CreateGameAssets(out EnemyConfig enemy, out WaveConfig wave,
            out TowerCatalog catalog, out GameObject enemyPrefab)
        {
            enemy = LoadOrCreate<EnemyConfig>(Root + "/Data/EnemyConfig.asset");
            enemy.maxHealth=100; enemy.moveSpeed=1.5f; enemy.reward=10;
            wave = LoadOrCreate<WaveConfig>(Root + "/Data/WaveConfig.asset");
            wave.enemyCount=10; wave.spawnInterval=.8f; wave.completionReward=25;
            wave.waves = new[]
            {
                new WaveDefinition { enemyCount = 6, healthMultiplier = .8f, spawnInterval = .9f, completionReward = 15 },
                new WaveDefinition { enemyCount = 8, healthMultiplier = .9f, spawnInterval = .85f, completionReward = 18 },
                new WaveDefinition { enemyCount = 10, healthMultiplier = 1f, spawnInterval = .8f, completionReward = 20 },
                new WaveDefinition { enemyCount = 11, healthMultiplier = 1.1f, spawnInterval = .75f, completionReward = 22 },
                new WaveDefinition { enemyCount = 12, healthMultiplier = 1.2f, spawnInterval = .7f, completionReward = 24 },
                new WaveDefinition { enemyCount = 14, healthMultiplier = 1.3f, spawnInterval = .65f, completionReward = 26 },
                new WaveDefinition { enemyCount = 15, healthMultiplier = 1.45f, spawnInterval = .6f, completionReward = 28 },
                new WaveDefinition { enemyCount = 16, healthMultiplier = 1.6f, spawnInterval = .55f, completionReward = 30 },
                new WaveDefinition { enemyCount = 18, healthMultiplier = 1.8f, spawnInterval = .5f, completionReward = 32 },
                new WaveDefinition { enemyCount = 20, healthMultiplier = 2f, spawnInterval = .45f, completionReward = 35 }
            };
            Sprite enemySprite=SpriteAt(Root+"/Art/Units/enemy1.png");
            Sprite tile=SpriteAt(Root+"/Art/Levels/tile1.png");
            GameObject projectile=new("Projectile");
            SpriteRenderer pr=projectile.AddComponent<SpriteRenderer>();pr.sprite=enemySprite;pr.color=Color.yellow;pr.sortingOrder=20;projectile.transform.localScale=Vector3.one*.18f;projectile.AddComponent<Projectile>();
            GameObject projectilePrefab=SavePrefab(projectile,Root+"/Prefabs/Projectile.prefab");
            GameObject enemyGo=new("Enemy");SpriteRenderer er=enemyGo.AddComponent<SpriteRenderer>();er.sprite=enemySprite;er.sortingOrder=10;enemyGo.AddComponent<CircleCollider2D>();
            GameObject hb=SpriteObject("HealthBack",tile,new Color(.18f,.04f,.22f),11);hb.transform.SetParent(enemyGo.transform);hb.transform.localPosition=new Vector3(0,.65f);hb.transform.localScale=new Vector3(.7f,.08f,1);
            GameObject hf=SpriteObject("HealthFill",tile,new Color(.4f,1,.35f),12);hf.transform.SetParent(hb.transform);hf.transform.localPosition=new Vector3(-.5f,0);Enemy ec=enemyGo.AddComponent<Enemy>();Set(ec,"body",er);Set(ec,"healthFill",hf.transform);
            enemyPrefab=SavePrefab(enemyGo,Root+"/Prefabs/Enemy.prefab");

            TowerConfig rapid=CreateTowerConfig("Rapid","Ice Tower","Slows one enemy by 30% for 1.25s",35,SpriteAt(Root+"/Art/Towers/rapid_tower.png"),new Color(.2f,1,.35f,.16f),AttackAnimationStyle.Pulse,new[]{new TowerLevelConfig{damage=12,attackRate=2.5f,range=2.8f,upgradeCost=55,projectileSpeed=11},new TowerLevelConfig{damage=18,attackRate=3.2f,range=3.2f,upgradeCost=0,projectileSpeed=13}});
            rapid.chillsTarget = true;
            rapid.towerPrefab=CreateTowerPrefab("RapidTower",rapid.icon,typeof(ProjectileAttack),projectilePrefab,Color.green);
            TowerConfig cannon=CreateTowerConfig("Cannon","fireTower","Burst damage to all enemies in range",55,SpriteAt(Root+"/Art/Towers/cannon_tower.png"),new Color(1,.65f,.1f,.16f),AttackAnimationStyle.Recoil,new[]{new TowerLevelConfig{damage=45,attackRate=.55f,range=2.4f,effectRadius=2.4f,upgradeCost=85,damageDelay=.12f},new TowerLevelConfig{damage=70,attackRate=.7f,range=2.8f,effectRadius=2.8f}});
            cannon.towerPrefab=CreateTowerPrefab("CannonTower",cannon.icon,typeof(AreaAttack),null,new Color(1,.7f,.1f,.25f));
            TowerConfig beam=CreateTowerConfig("Beam","ElectroTower","Instant beam hits one enemy",50,SpriteAt(Root+"/Art/Towers/beam_tower.png"),new Color(.1f,.8f,1,.16f),AttackAnimationStyle.Glow,new[]{new TowerLevelConfig{damage=24,attackRate=1.1f,range=3.5f,effectRadius=0,upgradeCost=75},new TowerLevelConfig{damage=38,attackRate=1.4f,range=4.1f,effectRadius=0}});
            beam.towerPrefab=CreateTowerPrefab("BeamTower",beam.icon,typeof(BeamAttack),null,Color.cyan);
            TowerConfig basic=CreateTowerConfig("Base","Base Tower","Single projectiles",25,SpriteAt(Root+"/Art/Units/tower1.png"),new Color(.4f,1,.5f,.16f),AttackAnimationStyle.Recoil,new[]{new TowerLevelConfig{damage=5,attackRate=1f,range=4.1f,upgradeCost=0,projectileSpeed=9f,effectRadius=0}});
            basic.towerPrefab=CreateTowerPrefab("BaseTower",basic.icon,typeof(ProjectileAttack),projectilePrefab,Color.yellow);
            catalog=LoadOrCreate<TowerCatalog>(Root+"/Data/TowerCatalog.asset");catalog.towers=new[]{basic,rapid,cannon,beam};
            foreach(Object o in new Object[]{enemy,wave,basic,rapid,cannon,beam,catalog})EditorUtility.SetDirty(o);
        }

        private static TowerConfig CreateTowerConfig(string id,string title,string description,int cost,Sprite sprite,Color color,AttackAnimationStyle style,TowerLevelConfig[] levels)
        {
            TowerConfig c=LoadOrCreate<TowerConfig>($"{Root}/Data/{id}Tower.asset");c.id=id.ToLowerInvariant();c.displayName=title;c.description=description;c.buildCost=cost;c.icon=sprite;c.rangeColor=color;c.animationStyle=style;c.levels=levels;foreach(TowerLevelConfig level in levels)level.sprite=sprite;return c;
        }

        private static GameObject CreateTowerPrefab(string name,Sprite sprite,System.Type attackType,GameObject projectile,Color effectColor)
        {
            Sprite circle=SpriteAt(Root+"/Art/Units/enemy1.png");GameObject root=new(name);GameObject visual=SpriteObject("Visual",sprite,Color.white,8);visual.transform.SetParent(root.transform);visual.transform.localPosition=Vector3.zero;visual.transform.localScale=Vector3.one;SpriteRenderer body=visual.GetComponent<SpriteRenderer>();root.AddComponent<CircleCollider2D>();
            Transform fire=new GameObject("FirePoint").transform;fire.SetParent(root.transform);fire.localPosition=new Vector3(0,.55f);
            GameObject ar=SpriteObject("AttackRange",circle,Color.white,2);ar.transform.SetParent(root.transform);ar.transform.localPosition=Vector3.zero;GameObject ef=SpriteObject("EffectRange",circle,Color.white,3);ef.transform.SetParent(root.transform);ef.transform.localPosition=Vector3.zero;
            TowerRangeView rv=root.AddComponent<TowerRangeView>();Set(rv,"attackRange",ar.GetComponent<SpriteRenderer>());Set(rv,"effectRange",ef.GetComponent<SpriteRenderer>());
            TowerTargeting targeting=root.AddComponent<TowerTargeting>();Animator animator=root.AddComponent<Animator>();TowerAnimationController anim=root.AddComponent<TowerAnimationController>();Set(anim,"visual",body.transform);Set(anim,"animator",animator);
            TowerAttackBehaviour attack=(TowerAttackBehaviour)root.AddComponent(attackType);
            if(attack is ProjectileAttack)Set(attack,"projectilePrefab",projectile);
            if(attack is BeamAttack beam){LineRenderer line=root.AddComponent<LineRenderer>();line.positionCount=2;line.startWidth=.09f;line.endWidth=.03f;line.material=new Material(Shader.Find("Sprites/Default"));line.startColor=effectColor;line.endColor=Color.white;line.sortingOrder=20;line.enabled=false;Set(beam,"beam",line);}
            if(attack is AreaAttack area){GameObject pulse=SpriteObject("Pulse",circle,effectColor,15);pulse.transform.SetParent(root.transform);pulse.transform.localPosition=Vector3.zero;pulse.SetActive(false);Set(area,"pulse",pulse.GetComponent<SpriteRenderer>());}
            TowerController controller=root.AddComponent<TowerController>();Set(controller,"body",body);Set(controller,"firePoint",fire);Set(controller,"targeting",targeting);Set(controller,"rangeView",rv);Set(controller,"animationController",anim);Set(controller,"attackBehaviour",attack);
            TowerSpineImporter.ConfigureTower(root, name);
            return SavePrefab(root,$"{Root}/Prefabs/{name}.prefab");
        }

        private static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset) return asset;
            if (File.Exists(path)) AssetDatabase.DeleteAsset(path);
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static GameObject SavePrefab(GameObject source, string path)
        {
            PrefabUtility.SaveAsPrefabAsset(source, path);
            Object.DestroyImmediate(source);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            return AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }

        private static void BuildLevel(EnemyConfig enemyConfig, WaveConfig waveConfig,TowerCatalog catalog,GameObject enemyPrefab)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Sprite backgroundSprite = SpriteAt(Root + "/Art/Levels/background.png");
            Sprite tileSprite = SpriteAt(Root + "/Art/Levels/tile1.png");
            Sprite spotSprite = SpriteAt(Root + "/Art/Levels/tower_place.png");
            Sprite towerSprite = SpriteAt(Root + "/Art/Units/tower1.png");

            Camera camera = new GameObject("Main Camera").AddComponent<Camera>();
            camera.tag = "MainCamera"; camera.orthographic = true; camera.orthographicSize = 9.6f;
            camera.transform.position = new Vector3(0, 0, -10); camera.backgroundColor = DeepPurple;

            GameObject background = SpriteObject("Background", backgroundSprite, Color.white, -20);
            background.transform.localScale = Vector3.one;

            Vector2[] points =
            {
                new(-3.2f, -9.0f), new(-3.2f, -5.0f), new(0f, -5.0f), new(0f, -0.5f),
                new(3.1f, -0.5f), new(3.1f, 4.3f), new(0.2f, 4.3f), new(0.2f, 8.8f)
            };
            Transform routeRoot = new GameObject("Route").transform;
            Transform[] route = new Transform[points.Length];
            for (int i = 0; i < points.Length; i++)
            {
                route[i] = new GameObject($"Waypoint_{i:00}").transform;
                route[i].SetParent(routeRoot); route[i].position = points[i];
                if (i > 0) DrawPath(points[i - 1], points[i], tileSprite, routeRoot);
            }

            GameObject baseGlow = SpriteObject("Base", towerSprite, new Color(0.25f, 0.9f, 1f), 5);
            baseGlow.transform.position = points[^1]; baseGlow.transform.localScale = Vector3.one * 1.25f;

            TowerFactory towerFactory = new GameObject("TowerFactory").AddComponent<TowerFactory>();

            Vector2[] spots = { new(-1.65f, -6.5f), new(1.6f, -3.25f), new(1.5f, 1.35f), new(-1.6f, 6.25f) };
            foreach (Vector2 position in spots)
            {
                GameObject spot = SpriteObject("BuildSpot", spotSprite, new Color(0.8f, 0.85f, 1f), 4);
                spot.transform.position = position; spot.transform.localScale = Vector3.one * 5f;
                CircleCollider2D collider = spot.AddComponent<CircleCollider2D>(); collider.radius = 0.18f;
                BuildSpot build = spot.AddComponent<BuildSpot>();
                Set(build, "towerFactory", towerFactory);
                Set(build, "marker", spot.GetComponent<SpriteRenderer>());
            }

            GameObject systems = new("Game Systems");
            Economy economy = systems.AddComponent<Economy>();
            BaseHealth health = systems.AddComponent<BaseHealth>();
            WaveController wave = systems.AddComponent<WaveController>();
            Set(wave, "config", waveConfig); Set(wave, "enemyConfig", enemyConfig);
            Set(wave, "enemyPrefab", enemyPrefab); Set(wave, "route", route);
            GameUI ui = BuildUI(catalog);
            GameController controller = systems.AddComponent<GameController>();
            Set(controller, "economy", economy); Set(controller, "baseHealth", health);
            Set(controller, "waveController", wave); Set(controller, "gameUI", ui);

            EditorSceneManager.SaveScene(scene, Root + "/Scenes/Level01.unity");
        }

        private static void DrawPath(Vector2 start, Vector2 end, Sprite sprite, Transform root)
        {
            float distance = Vector2.Distance(start, end);
            int count = Mathf.CeilToInt(distance / 0.82f);
            for (int i = 0; i <= count; i++)
            {
                Vector2 position = Vector2.Lerp(start, end, (float)i / count);
                GameObject shadow = SpriteObject("PathShadow", sprite, new Color(0.25f, 0.08f, 0.38f), -3);
                shadow.transform.SetParent(root); shadow.transform.position = position + new Vector2(0, -0.13f);
                shadow.transform.localScale = Vector3.one * 1.07f;
                GameObject tile = SpriteObject("PathTile", sprite, Purple, -2);
                tile.transform.SetParent(root); tile.transform.position = position;
                tile.transform.localScale = Vector3.one * 1.04f;
            }
        }

        private static GameUI BuildUI(TowerCatalog catalog)
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            Canvas canvas = new GameObject("Canvas").AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvas.gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920); scaler.matchWidthOrHeight = 0.5f;
            canvas.gameObject.AddComponent<GraphicRaycaster>();
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

            RectTransform safe = Rect("SafeArea", canvas.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            safe.gameObject.AddComponent<SafeAreaLayout>();
            GameObject top = ImagePanel("TopBar", safe, Panel, new Vector2(0.04f, 0.91f), new Vector2(0.96f, 0.98f));
            Text gold = TextUI("Gold", top.transform, "Gold: 100", 38, TextAnchor.MiddleLeft,
                new Vector2(0.03f, 0), new Vector2(0.38f, 1));
            Text health = TextUI("Health", top.transform, "Base: 5", 38, TextAnchor.MiddleCenter,
                new Vector2(0.35f, 0), new Vector2(0.66f, 1));
            Text waveLabel = TextUI("Wave", top.transform, "Build phase", 34, TextAnchor.MiddleRight,
                new Vector2(0.64f, 0), new Vector2(0.97f, 1));
            Text enemies = TextUI("Enemies", safe, "Wave 1/1", 34, TextAnchor.MiddleCenter,
                new Vector2(0.04f, 0.84f), new Vector2(0.62f, 0.9f));
            Button testProfile = ButtonUI("TestProfile", safe, "Test Profile", new Color(.65f,.65f,.7f),
                new Vector2(.65f,.845f), new Vector2(.96f,.90f));
            testProfile.GetComponentInChildren<Text>().fontSize = 30;

            Button start = ButtonUI("StartWave", safe, "START WAVE", new Color(0.45f, 0.88f, 0.25f),
                new Vector2(0.18f, 0.04f), new Vector2(0.82f, 0.115f));
            Button pause = ButtonUI("Pause", safe, "PAUSE", new Color(0.35f, 0.45f, 0.7f),
                new Vector2(.65f,.785f), new Vector2(.80f,.835f));
            Button speed = ButtonUI("Speed", safe, "SPEED ×1", new Color(0.45f, 0.65f, 0.35f),
                new Vector2(.81f,.785f), new Vector2(.96f,.835f));

            GameObject buildMenu = ImagePanel("BuildMenu", safe, Panel, new Vector2(0.04f, 0.12f), new Vector2(0.96f, 0.40f));
            TextUI("BuildTitle", buildMenu.transform, "CHOOSE TOWER", 34, TextAnchor.MiddleCenter,new Vector2(0,0.72f),new Vector2(1,1));
            Button[] towerButtons=new Button[catalog.towers.Length];Text[] towerTexts=new Text[catalog.towers.Length];
            Color[] towerColors={new(.2f,.8f,.3f),new(.25f,.8f,.95f),new(1f,.62f,.12f),new(.12f,.7f,1f)};
            for(int i=0;i<towerButtons.Length;i++)
            {
                float x=.03f+(i%2)*.485f;
                float y=.39f-(i/2)*.33f;
                towerButtons[i]=ButtonUI($"Tower{i}",buildMenu.transform,"Tower",towerColors[i%towerColors.Length],new Vector2(x,y),new Vector2(x+.455f,y+.29f));
                towerTexts[i]=towerButtons[i].GetComponentInChildren<Text>();
            }

            GameObject action = ImagePanel("ActionPanel", safe, Panel, new Vector2(0.08f, 0.12f), new Vector2(0.92f, 0.31f));
            Text actionTitle = TextUI("Title", action.transform, "Tower", 42, TextAnchor.MiddleLeft,
                new Vector2(0.06f, 0.60f), new Vector2(0.94f, 0.95f));
            Text actionDetails = TextUI("Details", action.transform, "", 30, TextAnchor.MiddleLeft,
                new Vector2(0.06f, 0.36f), new Vector2(0.94f, 0.64f));
            Button actionButton = ButtonUI("Action", action.transform, "UPGRADE", new Color(0.63f, 0.36f, 0.93f),
                new Vector2(0.06f, 0.06f), new Vector2(0.94f, 0.36f));
            Text actionButtonText = actionButton.GetComponentInChildren<Text>();

            GameObject result = ImagePanel("ResultPanel", canvas.transform, new Color(0.06f, 0.015f, 0.14f, 0.97f),
                Vector2.zero, Vector2.one);
            Text resultText = TextUI("Result", result.transform, "VICTORY", 84, TextAnchor.MiddleCenter,
                new Vector2(0.1f, 0.48f), new Vector2(0.9f, 0.68f));
            Button restart = ButtonUI("Restart", result.transform, "PLAY AGAIN", new Color(0.63f, 0.36f, 0.93f),
                new Vector2(0.18f, 0.35f), new Vector2(0.82f, 0.43f));

            GameObject tutorial = ImagePanel("TutorialPanel", safe, new Color(0.08f, 0.12f, 0.24f, 0.94f),
                new Vector2(0.04f, 0.42f), new Vector2(0.96f, 0.58f));
            tutorial.GetComponent<Image>().raycastTarget = false;
            Text tutorialText = TextUI("TutorialText", tutorial.transform, "", 28, TextAnchor.MiddleCenter,
                new Vector2(0.06f, 0.38f), new Vector2(0.94f, 0.95f));
            tutorialText.raycastTarget = false;
            Button tutorialRepeat = ButtonUI("TutorialRepeat", tutorial.transform, "REPEAT HINTS", new Color(0.25f, 0.55f, 0.9f),
                new Vector2(0.06f, 0.08f), new Vector2(0.49f, 0.32f));
            Button tutorialSkip = ButtonUI("TutorialSkip", tutorial.transform, "SKIP", new Color(0.45f, 0.45f, 0.5f),
                new Vector2(0.51f, 0.08f), new Vector2(0.94f, 0.32f));

            GameObject pausePanel = ImagePanel("PausePanel", safe, new Color(0.06f, 0.015f, 0.14f, 0.97f),
                new Vector2(0.18f, 0.37f), new Vector2(0.82f, 0.63f));
            TextUI("PauseTitle", pausePanel.transform, "PAUSED", 42, TextAnchor.MiddleCenter,
                new Vector2(0.06f, 0.38f), new Vector2(0.94f, 0.95f));
            Button resume = ButtonUI("Resume", pausePanel.transform, "RESUME", new Color(0.25f, 0.55f, 0.9f),
                new Vector2(0.08f, 0.08f), new Vector2(0.47f, 0.30f));
            Button pauseSpeed = ButtonUI("PauseSpeed", pausePanel.transform, "SPEED ×1", new Color(0.45f, 0.65f, 0.35f),
                new Vector2(0.53f, 0.08f), new Vector2(0.92f, 0.30f));

            GameUI ui = canvas.gameObject.AddComponent<GameUI>();
            Set(ui, "goldText", gold); Set(ui, "healthText", health); Set(ui, "waveText", waveLabel);
            Set(ui, "enemiesText", enemies); Set(ui, "startWaveButton", start); Set(ui, "actionPanel", action);
            Set(ui, "testProfileButton", testProfile);
            Set(ui, "pauseButton", pause); Set(ui, "speedButton", speed); Set(ui, "pausePanel", pausePanel);
            Set(ui, "resumeButton", resume); Set(ui, "pauseSpeedButton", pauseSpeed);
            Set(ui, "actionTitle", actionTitle); Set(ui, "actionDetails", actionDetails);
            Set(ui, "actionButton", actionButton); Set(ui, "actionButtonText", actionButtonText);
            Set(ui,"towerCatalog",catalog);Set(ui,"buildMenu",buildMenu);SetArray(ui,"towerButtons",towerButtons);SetArray(ui,"towerButtonTexts",towerTexts);
            Set(ui, "resultPanel", result); Set(ui, "resultText", resultText); Set(ui, "restartButton", restart);
            Set(ui, "tutorialPanel", tutorial); Set(ui, "tutorialText", tutorialText);
            Set(ui, "tutorialRepeatButton", tutorialRepeat); Set(ui, "tutorialSkipButton", tutorialSkip);
            action.SetActive(false);
            buildMenu.SetActive(false);
            result.SetActive(false);
            pausePanel.SetActive(false);
            return ui;
        }

        private static void BuildBootstrap()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            new GameObject("Bootstrap").AddComponent<BootstrapLoader>();
            EditorSceneManager.SaveScene(scene, Root + "/Scenes/Bootstrap.unity");
        }

        private static Sprite SpriteAt(string path) => AssetDatabase.LoadAssetAtPath<Sprite>(path);

        private static GameObject SpriteObject(string name, Sprite sprite, Color color, int order)
        {
            GameObject go = new(name);
            SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite; renderer.color = color; renderer.sortingOrder = order;
            return go;
        }

        private static RectTransform Rect(string name, Transform parent, Vector2 min, Vector2 max,
            Vector2 offsetMin, Vector2 offsetMax)
        {
            RectTransform rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false); rect.anchorMin = min; rect.anchorMax = max;
            rect.offsetMin = offsetMin; rect.offsetMax = offsetMax;
            return rect;
        }

        private static GameObject ImagePanel(string name, Transform parent, Color color, Vector2 min, Vector2 max)
        {
            RectTransform rect = Rect(name, parent, min, max, Vector2.zero, Vector2.zero);
            rect.gameObject.AddComponent<Image>().color = color;
            return rect.gameObject;
        }

        private static Text TextUI(string name, Transform parent, string value, int size, TextAnchor alignment,
            Vector2 min, Vector2 max)
        {
            RectTransform rect = Rect(name, parent, min, max, Vector2.zero, Vector2.zero);
            Text text = rect.gameObject.AddComponent<Text>();
            text.font = font; text.text = value; text.fontSize = size; text.alignment = alignment;
            text.color = Color.white; text.resizeTextForBestFit = true; text.resizeTextMinSize = 18;
            return text;
        }

        private static Button ButtonUI(string name, Transform parent, string value, Color color,
            Vector2 min, Vector2 max)
        {
            GameObject buttonObject = ImagePanel(name, parent, color, min, max);
            Button button = buttonObject.AddComponent<Button>();
            ColorBlock colors = button.colors; colors.highlightedColor = color * 1.15f;
            colors.pressedColor = color * 0.8f; colors.disabledColor = new Color(0.25f, 0.22f, 0.3f, 0.75f);
            button.colors = colors;
            TextUI("Label", buttonObject.transform, value, 38, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one);
            return button;
        }

        private static void Set(Object target, string property, Object value)
        {
            SerializedObject serialized = new(target);
            serialized.FindProperty(property).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void Set(Object target, string property, Transform[] values)
        {
            SerializedObject serialized = new(target);
            SerializedProperty array = serialized.FindProperty(property);
            array.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetArray<T>(Object target,string property,T[] values) where T:Object
        {
            SerializedObject serialized=new(target);SerializedProperty array=serialized.FindProperty(property);array.arraySize=values.Length;for(int i=0;i<values.Length;i++)array.GetArrayElementAtIndex(i).objectReferenceValue=values[i];serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}

namespace FirstGameEditor
{
    public sealed class ResponsiveWebGLPostprocessor : IPostprocessBuildWithReport
    {
        public int callbackOrder => 100;

        public void OnPostprocessBuild(BuildReport report)
        {
            if (report.summary.platform != BuildTarget.WebGL) return;
            string indexPath = Path.Combine(report.summary.outputPath, "index.html");
            if (!File.Exists(indexPath)) return;
            string html = File.ReadAllText(indexPath);
            const string fixedSize = "fullscreenContainer.style.width = \"1080px\";\n        fullscreenContainer.style.height = \"1920px\";";
            const string responsiveSize = "const aspect = 1080 / 1920;\n        const height = Math.min(window.innerHeight, window.innerWidth / aspect);\n        fullscreenContainer.style.height = height + \"px\";\n        fullscreenContainer.style.width = (height * aspect) + \"px\";";
            if (html.Contains(fixedSize)) File.WriteAllText(indexPath, html.Replace(fixedSize, responsiveSize));
        }
    }
}
