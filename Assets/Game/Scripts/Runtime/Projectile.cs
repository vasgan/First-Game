using UnityEngine;

namespace FirstGame
{
    public sealed class Projectile : MonoBehaviour
    {
        private Enemy target;
        private int targetSpawnVersion;
        private int damage;
        private float speed;
        private bool chillsTarget;
        private SpriteRenderer visual;
        private Color normalColor;
        private System.Action<Projectile> returned;
        public bool InUse { get; private set; }
        public void Launch(Vector3 origin, Enemy newTarget, int newDamage, float newSpeed,
            System.Action<Projectile> onReturned, bool applyChill = false)
        {
            transform.position = origin;
            target = newTarget;
            targetSpawnVersion = newTarget.SpawnVersion;
            damage = newDamage;
            speed = newSpeed;
            chillsTarget = applyChill;
            returned = onReturned;
            if (!visual && TryGetComponent(out visual)) normalColor = visual.color;
            if (visual) visual.color = chillsTarget ? new Color(0.25f, 0.8f, 1f) : normalColor;
            InUse = true;
            gameObject.SetActive(true);
        }
        private void Update()
        {
            if (!InUse) return;
            if (!target || !target.IsAlive || target.SpawnVersion != targetSpawnVersion) { Return(); return; }
            transform.position = Vector3.MoveTowards(transform.position, target.transform.position, speed * Time.deltaTime);
            if ((transform.position - target.transform.position).sqrMagnitude > 0.01f) return;
            target.TakeDamage(damage);
            if (chillsTarget && target && target.SpawnVersion == targetSpawnVersion) target.ApplyChill();
            Return();
        }
        internal void Return()
        {
            if (!InUse) return;
            InUse = false;
            var onReturned = returned;
            returned = null;
            target = null;
            targetSpawnVersion = 0;
            chillsTarget = false;
            if (visual) visual.color = normalColor;
            gameObject.SetActive(false);
            onReturned?.Invoke(this);
        }
    }
}
