using System;
using System.Reflection;
using FirstGame;
using UnityEditor;
using UnityEngine;

namespace FirstGameEditor
{
    public static class TowerTargetingValidation
    {
        [MenuItem("First Game/Validate Tower Targeting")]
        public static void Validate()
        {
            GameObject targeterObject = new("Targeting validation");
            GameObject firstWaypoint = new("First waypoint");
            GameObject secondWaypoint = new("Second waypoint");
            GameObject thirdWaypoint = new("Third waypoint");
            GameObject firstEnemy = null;
            GameObject secondEnemy = null;
            GameObject thirdEnemy = null;

            try
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Enemy.prefab");
                Require(prefab != null, "Enemy prefab must exist");
                firstWaypoint.transform.position = Vector3.zero;
                secondWaypoint.transform.position = new Vector3(10f, 0f);
                thirdWaypoint.transform.position = new Vector3(10f, 10f);
                Transform[] route = { firstWaypoint.transform, secondWaypoint.transform, thirdWaypoint.transform };
                EnemyConfig config = ScriptableObject.CreateInstance<EnemyConfig>();

                firstEnemy = UnityEngine.Object.Instantiate(prefab);
                secondEnemy = UnityEngine.Object.Instantiate(prefab);
                Enemy aheadOnSegment = firstEnemy.GetComponent<Enemy>();
                Enemy behindOnSegment = secondEnemy.GetComponent<Enemy>();
                aheadOnSegment.Spawn(config, route, _ => { });
                behindOnSegment.Spawn(config, route, _ => { });
                aheadOnSegment.transform.position = new Vector3(8f, 0f);
                behindOnSegment.transform.position = new Vector3(2f, 0f);

                TowerTargeting targeting = targeterObject.AddComponent<TowerTargeting>();
                Require(targeting.FindTarget(Vector3.zero, 100f, TargetingRule.ClosestToBase) == aheadOnSegment,
                    "Closest-to-base targeting must distinguish progress inside a route segment");

                thirdEnemy = UnityEngine.Object.Instantiate(prefab);
                Enemy pastTurn = thirdEnemy.GetComponent<Enemy>();
                pastTurn.Spawn(config, route, _ => { });
                pastTurn.transform.position = new Vector3(10f, 2f);
                FieldInfo waypointIndex = typeof(Enemy).GetField("waypointIndex",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                waypointIndex.SetValue(pastTurn, 2);
                Require(targeting.FindTarget(Vector3.zero, 100f, TargetingRule.ClosestToBase) == pastTurn,
                    "Closest-to-base targeting must preserve progress after a turn");

                UnityEngine.Object.DestroyImmediate(config);
                Debug.Log("FIRST_GAME_TOWER_TARGETING_VALIDATION_PASSED");
            }
            finally
            {
                if (thirdEnemy != null) UnityEngine.Object.DestroyImmediate(thirdEnemy);
                if (secondEnemy != null) UnityEngine.Object.DestroyImmediate(secondEnemy);
                if (firstEnemy != null) UnityEngine.Object.DestroyImmediate(firstEnemy);
                UnityEngine.Object.DestroyImmediate(thirdWaypoint);
                UnityEngine.Object.DestroyImmediate(secondWaypoint);
                UnityEngine.Object.DestroyImmediate(firstWaypoint);
                UnityEngine.Object.DestroyImmediate(targeterObject);
            }
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new Exception("Tower targeting validation failed: " + message);
        }
    }
}
