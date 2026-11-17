using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FirstGame
{
    public sealed class Enemy : MonoBehaviour
    {
        public static readonly List<Enemy> Active = new();
        private const float ChillDuration = 1.25f;
        private const float ChilledSpeedMultiplier = 0.7f;
        private const float HitPulseDuration = 0.1f;
        private const float DeathFeedbackDuration = 0.16f;
        private static readonly Color ChillTint = new(0.45f, 0.85f, 1f);
        private static readonly Color ChillHitTint = new(0.2f, 0.75f, 1f);

        [SerializeField] private SpriteRenderer body;
        [SerializeField] private Transform healthFill;

        private EnemyConfig config;
        private Transform[] route;
        private int waypointIndex;
        private int health;
        private int maxHealth;
        private int spawnVersion;
        private float bobTime;
        private float chillRemaining;
        private Color normalColor;
        private Action<Enemy> finished;

        public bool IsAlive { get; private set; }
        public int Health => health;
        public int MaxHealth => maxHealth;
        public int SpawnVersion => spawnVersion;
        public float RouteProgress => waypointIndex;
        public float RemainingRouteDistance
        {
            get
            {
                if (route == null || waypointIndex >= route.Length) return 0f;

                float distance = Vector3.Distance(transform.position, route[waypointIndex].position);
                for (int index = waypointIndex; index < route.Length - 1; index++)
                    distance += Vector3.Distance(route[index].position, route[index + 1].position);

                return distance;
            }
        }
        internal bool HasRequiredReferences => body && healthFill;

        public void Spawn(EnemyConfig enemyConfig, Transform[] waypoints, Action<Enemy> onFinished, int? waveMaxHealth = null)
        {
            StopAllCoroutines();
            ClearChill();
            normalColor = body.color;
            config = enemyConfig;
            route = waypoints;
            finished = onFinished;
            waypointIndex = 1;
            maxHealth = Mathf.Max(1, waveMaxHealth ?? config.maxHealth);
            health = maxHealth;
            spawnVersion++;
            IsAlive = true;
            transform.position = route[0].position;
            transform.localScale = Vector3.one;
            healthFill.localScale = Vector3.one;
            gameObject.SetActive(true);
            Active.Add(this);
        }

        private void Update()
        {
            if (!IsAlive || !GameController.Instance.CanInteract) return;
            if (waypointIndex >= route.Length)
            {
                ReachBase();
                return;
            }

            Move(Time.deltaTime);
            if (Vector3.SqrMagnitude(transform.position - route[waypointIndex].position) < 0.0025f)
                waypointIndex++;

            bobTime += Time.deltaTime * 6f;
            float pulse = 1f + Mathf.Sin(bobTime) * 0.04f;
            body.transform.localScale = new Vector3(pulse, 2f - pulse, 1f);
        }

        private void Move(float deltaTime)
        {
            float chilledTime = Mathf.Min(chillRemaining, deltaTime);
            float distance = config.moveSpeed * (deltaTime - chilledTime * (1f - ChilledSpeedMultiplier));
            transform.position = Vector3.MoveTowards(transform.position, route[waypointIndex].position, distance);

            if (chillRemaining <= 0f) return;
            if (chilledTime >= chillRemaining)
            {
                ClearChill();
                return;
            }

            chillRemaining -= chilledTime;
            body.color = Color.Lerp(ChillTint, ChillHitTint,
                Mathf.Clamp01(1f - (ChillDuration - chillRemaining) / 0.12f));
        }

        internal void ApplyChill()
        {
            GameController game = GameController.Instance;
            if (!IsAlive || (game && !game.CanInteract)) return;
            chillRemaining = ChillDuration;
            body.color = ChillHitTint;
        }

        private void ClearChill()
        {
            if (chillRemaining <= 0f) return;
            chillRemaining = 0f;
            body.color = normalColor;
        }

        public void TakeDamage(int amount)
        {
            GameController game = GameController.Instance;
            if (!IsAlive || (game && !game.CanInteract)) return;
            health = Mathf.Max(0, health - amount);
            healthFill.localScale = new Vector3((float)health / maxHealth, 1f, 1f);
            if (health == 0)
                Die();
            else
                StartCoroutine(ShowHitFeedback());
        }

        private void Die()
        {
            IsAlive = false;
            StartCoroutine(ShowDeathFeedback());
        }

        private IEnumerator ShowHitFeedback()
        {
            transform.localScale = Vector3.one * 1.16f;
            yield return new WaitForSeconds(HitPulseDuration);
            if (IsAlive) transform.localScale = Vector3.one;
        }

        private IEnumerator ShowDeathFeedback()
        {
            transform.localScale = Vector3.one * 1.25f;
            yield return new WaitForSeconds(DeathFeedbackDuration);
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
            ClearChill();
            Active.Remove(this);
            finished?.Invoke(this);
            gameObject.SetActive(false);
        }

        private void OnDestroy() => Active.Remove(this);
    }
}
