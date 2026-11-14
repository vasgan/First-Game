using System;
using System.IO;
using System.Linq;
using FirstGame;
using Spine.Unity;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace FirstGameEditor
{
    public static class TowerSpineImporter
    {
        private const string Folder = "Assets/Game/Animations/Towers";
        private static readonly string[] Prefabs = { "BaseTower", "RapidTower", "CannonTower", "BeamTower" };
        private static readonly string[] Colors = { "green", "blue", "red", "yellow" };

        [MenuItem("First Game/Import Tower Spine Animations")]
        public static void Install()
        {
            ImportAssets();
            foreach (string name in Prefabs)
            {
                string path = $"Assets/Game/Prefabs/{name}.prefab";
                GameObject root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    ConfigureTower(root, name);
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                }
                finally { PrefabUtility.UnloadPrefabContents(root); }
            }
            AssetDatabase.SaveAssets();
            Validate();
            Debug.Log("FIRST_GAME_SPINE_TOWERS_IMPORTED");
        }

        public static void ImportAssets()
        {
            // Create editor preferences before importing textures; Unity 6.6 forbids it inside an import callback.
            Spine.Unity.Editor.SpinePreferences.Load();
            string source = Path.Combine(Directory.GetCurrentDirectory(), "export 2");
            Directory.CreateDirectory(Folder);
            Copy(source, "units2.png", "units2.png");
            Copy(source, "units2.atlas", "units2.atlas.txt");
            foreach (string color in Colors) Copy(source, $"tower_{color}.json", $"tower_{color}.json");
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            TextureImporter textureImporter = (TextureImporter)AssetImporter.GetAtPath(Folder + "/units2.png");
            textureImporter.textureType = TextureImporterType.Default;
            textureImporter.npotScale = TextureImporterNPOTScale.None;
            textureImporter.mipmapEnabled = false;
            textureImporter.alphaIsTransparency = true;
            textureImporter.wrapMode = TextureWrapMode.Clamp;
            textureImporter.filterMode = FilterMode.Bilinear;
            textureImporter.textureCompression = TextureImporterCompression.Uncompressed;
            textureImporter.maxTextureSize = 2048;
            textureImporter.SaveAndReimport();

            string materialPath = Folder + "/units2_Material.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (!material)
            {
                material = new Material(Shader.Find("Spine/Skeleton"));
                AssetDatabase.CreateAsset(material, materialPath);
            }
            material.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(Folder + "/units2.png");
            material.SetFloat("_StraightAlphaInput", 1f);
            material.EnableKeyword("_STRAIGHT_ALPHA_INPUT");
            EditorUtility.SetDirty(material);

            SpineAtlasAsset atlas = LoadOrCreate<SpineAtlasAsset>(Folder + "/units2_Atlas.asset");
            atlas.atlasFile = AssetDatabase.LoadAssetAtPath<TextAsset>(Folder + "/units2.atlas.txt");
            atlas.materials = new[] { material };
            atlas.Clear();
            atlas.GetAtlas();
            EditorUtility.SetDirty(atlas);
            foreach (string color in Colors)
            {
                SkeletonDataAsset data = LoadOrCreate<SkeletonDataAsset>(DataPath(color));
                data.skeletonJSON = AssetDatabase.LoadAssetAtPath<TextAsset>($"{Folder}/tower_{color}.json");
                data.atlasAssets = new AtlasAssetBase[] { atlas };
                data.scale = .01f;
                data.defaultMix = .08f;
                data.Clear();
                if (data.GetSkeletonData(false) == null) throw new InvalidOperationException($"Cannot load {color} tower skeleton.");
                EditorUtility.SetDirty(data);
            }
            AssetDatabase.SaveAssets();
        }

        public static void ConfigureTower(GameObject root, string prefabName)
        {
            int index = Array.IndexOf(Prefabs, prefabName);
            if (index < 0) return;
            SkeletonDataAsset data = AssetDatabase.LoadAssetAtPath<SkeletonDataAsset>(DataPath(Colors[index]));
            if (!data) throw new InvalidOperationException("Import tower Spine assets before creating tower prefabs.");
            Transform visual = root.transform.Find("Visual");
            SpriteRenderer sprite = visual.GetComponent<SpriteRenderer>();
            if (sprite) Object.DestroyImmediate(sprite);
            Animator animator = root.GetComponent<Animator>();
            if (animator) Object.DestroyImmediate(animator);

            SkeletonAnimation skeleton = visual.GetComponent<SkeletonAnimation>();
            if (!skeleton) skeleton = visual.gameObject.AddComponent<SkeletonAnimation>();
            skeleton.skeletonDataAsset = data;
            skeleton.loop = true;
            skeleton.AnimationName = "idle_r_b";
            skeleton.Initialize(true);
            visual.localScale = Vector3.one * 1.2f;
            visual.GetComponent<MeshRenderer>().sortingOrder = 8;

            SerializedObject animation = new SerializedObject(root.GetComponent<TowerAnimationController>());
            animation.FindProperty("visual").objectReferenceValue = visual;
            animation.FindProperty("animator").objectReferenceValue = null;
            animation.FindProperty("skeletonAnimation").objectReferenceValue = skeleton;
            animation.ApplyModifiedPropertiesWithoutUndo();
            SerializedObject controller = new SerializedObject(root.GetComponent<TowerController>());
            controller.FindProperty("body").objectReferenceValue = null;
            controller.ApplyModifiedPropertiesWithoutUndo();
        }

        [MenuItem("First Game/Validate Tower Spine Animations")]
        public static void Validate()
        {
            for (int i = 0; i < Prefabs.Length; i++)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Game/Prefabs/{Prefabs[i]}.prefab");
                GameObject instance = Object.Instantiate(prefab);
                Enemy target = new GameObject("Animation validation target").AddComponent<Enemy>();
                try
                {
                    string configName = Prefabs[i];
                    TowerConfig config = AssetDatabase.LoadAssetAtPath<TowerConfig>($"Assets/Game/Data/{configName}.asset");
                    instance.GetComponent<TowerController>().Initialize(config);
                    SkeletonAnimation skeleton = instance.GetComponentInChildren<SkeletonAnimation>();
                    TowerAnimationController animation = instance.GetComponent<TowerAnimationController>();
                    if (!skeleton || !skeleton.valid) throw new InvalidOperationException($"Invalid Spine prefab: {configName}");
                    Vector3 originalScale = skeleton.transform.localScale;
                    animation.SetLevel(2);
                    foreach (Vector2 direction in new[] { new Vector2(-1,-1), new Vector2(-1,1), new Vector2(1,-1), new Vector2(1,1) })
                    {
                        target.transform.position = instance.transform.position + (Vector3)direction;
                        animation.PlayAttack(target, .3f);
                        string expected = "attack_" + (direction.x < 0 ? "l" : "r") + (direction.y > 0 ? "_t" : "_b");
                        if (skeleton.AnimationState.GetCurrent(0).Animation.Name != expected)
                            throw new InvalidOperationException($"Wrong attack direction: {configName}");
                        for (int frame = 0; frame < 20; frame++) skeleton.Update(.05f);
                        skeleton.LateUpdate();
                        if (!skeleton.AnimationState.GetCurrent(0).Animation.Name.StartsWith("idle_"))
                            throw new InvalidOperationException($"Attack did not return to idle: {configName}");
                        if ((skeleton.transform.localScale - originalScale * 1.1f).sqrMagnitude > .000001f)
                            throw new InvalidOperationException($"Level growth lost after animation: {configName}");
                        if (skeleton.GetComponent<MeshFilter>().sharedMesh.vertexCount == 0)
                            throw new InvalidOperationException($"Empty animated mesh: {configName}");
                    }
                    animation.SetLevel(3);
                    if ((skeleton.transform.localScale - originalScale * 1.21f).sqrMagnitude > .000001f)
                        throw new InvalidOperationException($"Incorrect third level growth: {configName}");
                }
                finally
                {
                    Object.DestroyImmediate(instance);
                    Object.DestroyImmediate(target.gameObject);
                }
            }
            Debug.Log("FIRST_GAME_SPINE_TOWERS_VALIDATED");
        }

        private static string DataPath(string color) => $"{Folder}/tower_{color}_SkeletonData.asset";

        private static void Copy(string source, string name, string destination)
        {
            string path = Path.Combine(Folder, destination);
            string sourcePath = Path.Combine(source, name);
            if (File.Exists(path) && File.ReadAllBytes(sourcePath).SequenceEqual(File.ReadAllBytes(path))) return;
            File.Copy(sourcePath, path, true);
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
