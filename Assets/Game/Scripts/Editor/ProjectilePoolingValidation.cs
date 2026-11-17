using System;
using System.Reflection;
using FirstGame;
using UnityEditor;
using UnityEngine;

namespace FirstGameEditor
{
    public static class ProjectilePoolingValidation
    {
        [MenuItem("First Game/Validate Projectile Pooling")]
        public static void Validate()
        {
            GameObject firstWaypoint = new("First waypoint");
            GameObject secondWaypoint = new("Second waypoint");
            GameObject enemyObject = null;
            GameObject projectileObject = null;
            EnemyConfig config = null;

            try
            {
                GameObject enemyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Enemy.prefab");
                GameObject projectilePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Projectile.prefab");
                Require(enemyPrefab != null, "Enemy prefab must exist");
                Require(projectilePrefab != null, "Projectile prefab must exist");

                firstWaypoint.transform.position = Vector3.zero;
                secondWaypoint.transform.position = Vector3.right * 10f;
                Transform[] route = { firstWaypoint.transform, secondWaypoint.transform };
                config = ScriptableObject.CreateInstance<EnemyConfig>();
                config.maxHealth = 100;

                enemyObject = UnityEngine.Object.Instantiate(enemyPrefab);
                Enemy enemy = enemyObject.GetComponent<Enemy>();
                enemy.Spawn(config, route, _ => { });

                projectileObject = UnityEngine.Object.Instantiate(projectilePrefab);
                Projectile projectile = projectileObject.GetComponent<Projectile>();
                projectile.Launch(enemy.transform.position, enemy, 10, 1f, _ => { });

                typeof(Enemy).GetMethod("Finish", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(enemy, null);
                Require(!enemy.gameObject.activeSelf, "Enemy must return to the pool before reuse");

                enemy.Spawn(config, route, _ => { });
                Require(enemy.Health == enemy.MaxHealth, "Reused enemy must start with full health");
                typeof(Projectile).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(projectile, null);

                Require(enemy.Health == enemy.MaxHealth,
                    "An old projectile must not damage the same enemy object after it is reused from the pool");
                Require(!projectile.InUse, "Projectile must return after detecting a reused enemy object");
                Debug.Log("FIRST_GAME_PROJECTILE_POOLING_VALIDATION_PASSED");
            }
            finally
            {
                if (config != null) UnityEngine.Object.DestroyImmediate(config);
                if (projectileObject != null) UnityEngine.Object.DestroyImmediate(projectileObject);
                if (enemyObject != null) UnityEngine.Object.DestroyImmediate(enemyObject);
                UnityEngine.Object.DestroyImmediate(secondWaypoint);
                UnityEngine.Object.DestroyImmediate(firstWaypoint);
            }
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new Exception("Projectile pooling validation failed: " + message);
        }
    }
}
