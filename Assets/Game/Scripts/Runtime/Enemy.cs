using System;
using System.Collections.Generic;
using UnityEngine;

namespace FirstGame
{
    public sealed class Enemy : MonoBehaviour
    {
        public static readonly List<Enemy> Active = new();

        [SerializeField] private SpriteRenderer body;
        [SerializeField] private Transform healthFill;

        private EnemyConfig config;
        private Transform[] route;
        private int waypointIndex;
        private int health;
        private int maxHealth;
        private float bobTime;
        private Action<Enemy> finished;

        public bool IsAlive { get; private set; }
        public int Health => health;
        public int MaxHealth => maxHealth;
        public float RouteProgress => waypointIndex;

        public void Spawn(EnemyConfig enemyConfig, Transform[] waypoints, Action<Enemy> onFinished, int? waveMaxHealth = null)
        {
            config = enemyConfig;
            route = waypoints;
            finished = onFinished;
            waypointIndex = 1;
            maxHealth = Mathf.Max(1, waveMaxHealth ?? config.maxHealth);
            health = maxHealth;
            IsAlive = true;
            transform.position = route[0].position;
            transform.localScale = Vector3.one;
            healthFill.localScale = Vector3.one;
            gameObject.SetActive(true);
            Active.Add(this);
        }

        private void Update()
        {
            if (!IsAlive || GameController.Instance.State == GameState.Lost) return;
            if (waypointIndex >= route.Length)
            {
                ReachBase();
                return;
            }

            transform.position = Vector3.MoveTowards(transform.position, route[waypointIndex].position,
                config.moveSpeed * Time.deltaTime);
            if (Vector3.SqrMagnitude(transform.position - route[waypointIndex].position) < 0.0025f)
                waypointIndex++;

            bobTime += Time.deltaTime * 6f;
            float pulse = 1f + Mathf.Sin(bobTime) * 0.04f;
            body.transform.localScale = new Vector3(pulse, 2f - pulse, 1f);
        }

        public void TakeDamage(int amount)
        {
            if (!IsAlive) return;
            health = Mathf.Max(0, health - amount);
            healthFill.localScale = new Vector3((float)health / maxHealth, 1f, 1f);
            StopAllCoroutines();
            StartCoroutine(Flash());
            if (health == 0) Die();
        }

        private System.Collections.IEnumerator Flash()
        {
            body.color = Color.white;
            yield return new WaitForSeconds(0.08f);
            body.color = Color.white;
        }

        private void Die()
        {
            GameController.Instance.Economy.Add(config.reward);
            Finish();
        }

        private void ReachBase()
        {
            GameController.Instance.BaseHealth.TakeDamage();
            Finish();
        }

        private void Finish()
        {
            IsAlive = false;
            Active.Remove(this);
            finished?.Invoke(this);
            gameObject.SetActive(false);
        }

        private void OnDestroy() => Active.Remove(this);
    }
}
