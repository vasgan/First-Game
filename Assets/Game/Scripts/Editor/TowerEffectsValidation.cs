using System;
using System.Collections.Generic;
using System.Reflection;
using FirstGame;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace FirstGameEditor
{
    public static class TowerEffectsValidation
    {
        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;

        [MenuItem("First Game/Validate Tower Effects")]
        public static void Validate()
        {
            var objects = new List<Object>();
            try
            {
                GameObject start = new("Start");
                GameObject end = new("End");
                objects.Add(start);
                objects.Add(end);
                end.transform.position = Vector3.right * 100f;
                Transform[] route = { start.transform, end.transform };

                EnemyConfig config = ScriptableObject.CreateInstance<EnemyConfig>();
                objects.Add(config);
                config.maxHealth = 200;
                config.moveSpeed = 4f;

                Enemy target = SpawnEnemy(objects, config, route);
                Enemy neighbor = SpawnEnemy(objects, config, route);
                Enemy outside = SpawnEnemy(objects, config, route);
                Projectile projectile = InstantiateAsset(objects, "Prefabs/Projectile.prefab").GetComponent<Projectile>();
                SpriteRenderer projectileVisual = projectile.GetComponent<SpriteRenderer>();
                Color projectileColor = projectileVisual.color;
                SpriteRenderer enemyVisual = target.GetComponent<SpriteRenderer>();
                Color enemyColor = enemyVisual.color;

                ProjectileAttack ice = CreateAttack<ProjectileAttack>(objects, "RapidTower");
                ProjectileAttack basic = CreateAttack<ProjectileAttack>(objects, "BaseTower");
                AreaAttack fire = CreateAttack<AreaAttack>(objects, "CannonTower");
                BeamAttack electro = CreateAttack<BeamAttack>(objects, "BeamTower");

                Shoot(ice, projectile, target);
                Require(target.Health == 188 && neighbor.Health == 200, "Ice deals damage to one enemy on impact.");
                Require(enemyVisual.color != enemyColor, "Ice hit is visible on the enemy.");
                Move(target, 0.75f, 2.1f);
                Shoot(ice, projectile, target);
                Move(target, 1f, 2.8f);
                Move(target, 0.5f, 1.7f);
                Require(enemyVisual.color == enemyColor, "Chill expires and clears its tint.");

                Shoot(basic, projectile, target);
                Require(target.Health == 171 && enemyVisual.color == enemyColor, "Base deals damage without chilling.");
                Require(projectileVisual.color == projectileColor, "A returned projectile restores its original tint.");
                Move(target, 0.25f, 1f);

                Shoot(ice, projectile, target);
                target.transform.position = Vector3.right;
                neighbor.transform.position = Vector3.right * 2.4f;
                outside.transform.position = Vector3.right * 2.41f;
                fire.Attack(outside);
                Require(target.Health == 114 && neighbor.Health == 155 && outside.Health == 200,
                    "Fire uses positions at impact and damages all live enemies within the radius once.");

                electro.Attack(target);
                Require(target.Health == 90 && neighbor.Health == 155 && outside.Health == 200,
                    "Electro deals instant damage to exactly one target.");
                Move(target, 0.5f, 1.4f);
                Move(neighbor, 0.5f, 2f);
                Require(target.Health == 90 && neighbor.Health == 155,
                    "Fire and Electro do not apply additional damage over time.");

                projectile.Launch(target.transform.position, target, 12, 11f, _ => { }, true);
                Invoke(target, "Finish");
                Require(!target.gameObject.activeSelf && enemyVisual.color == enemyColor,
                    "Returning the enemy to the pool clears chill and its tint.");
                target.Spawn(config, route, _ => { });
                Invoke(projectile, "Update");
                Require(!projectile.InUse && target.Health == 200 && enemyVisual.color == enemyColor,
                    "A stale Ice projectile cannot damage or chill a reused enemy object.");
                Move(target, 0.5f, 2f);

                Debug.Log("FIRST_GAME_TOWER_EFFECTS_VALIDATION_PASSED");
            }
            finally
            {
                for (int index = objects.Count - 1; index >= 0; index--)
                    Object.DestroyImmediate(objects[index]);
            }
        }

        private static Enemy SpawnEnemy(List<Object> objects, EnemyConfig config, Transform[] route)
        {
            Enemy enemy = InstantiateAsset(objects, "Prefabs/Enemy.prefab").GetComponent<Enemy>();
            enemy.Spawn(config, route, _ => { });
            return enemy;
        }

        private static T CreateAttack<T>(List<Object> objects, string name) where T : TowerAttackBehaviour
        {
            TowerConfig config = AssetDatabase.LoadAssetAtPath<TowerConfig>($"Assets/Game/Data/{name}.asset");
            Require(config && config.towerPrefab, $"Missing tower configuration {name}.");
            GameObject instance = Object.Instantiate(config.towerPrefab);
            objects.Add(instance);
            TowerController controller = instance.GetComponent<TowerController>();
            typeof(TowerController).GetField("config", PrivateInstance).SetValue(controller, config);
            T attack = instance.GetComponent<T>();
            attack.Initialize(new TowerAttackContext(controller, instance.transform, null));
            return attack;
        }

        private static GameObject InstantiateAsset(List<Object> objects, string path)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Game/{path}");
            Require(prefab, $"Missing prefab {path}.");
            GameObject instance = Object.Instantiate(prefab);
            objects.Add(instance);
            return instance;
        }

        private static void Shoot(ProjectileAttack attack, Projectile projectile, Enemy target)
        {
            var pool = (Queue<Projectile>)typeof(ProjectileAttack).GetField("pool", PrivateInstance).GetValue(attack);
            pool.Clear();
            pool.Enqueue(projectile);
            attack.transform.position = target.transform.position;
            attack.Attack(target);
            Require(projectile.InUse, "Projectile attack launches the pooled projectile.");
            Invoke(projectile, "Update");
            Require(!projectile.InUse, "Projectile returns after hitting.");
        }

        private static void Move(Enemy enemy, float seconds, float expectedDistance)
        {
            float startX = enemy.transform.position.x;
            Invoke(enemy, "Move", seconds);
            float distance = enemy.transform.position.x - startX;
            Require(Mathf.Abs(distance - expectedDistance) < 0.001f,
                $"Expected movement of {expectedDistance} in {seconds}s, got {distance}.");
        }

        private static void Invoke(object component, string method, params object[] args) =>
            component.GetType().GetMethod(method, PrivateInstance).Invoke(component, args);

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException("Tower effects validation failed. " + message);
        }
    }
}
