using System.Collections;
using UnityEngine;

namespace FirstGame
{
    public sealed class AreaAttack : TowerAttackBehaviour
    {
        [SerializeField] SpriteRenderer pulse;
        internal override bool HasRequiredReferences => pulse && pulse.sprite;
        public override void Attack(Enemy ignored)
        {
            TowerLevelConfig stats = Context.Tower.Stats;
            float radius = stats.effectRadius;
            Vector3 center = Context.Tower.transform.position;
            foreach (Enemy enemy in Enemy.Active.ToArray())
            {
                if (enemy && enemy.IsAlive && (enemy.transform.position - center).sqrMagnitude <= radius * radius)
                    enemy.TakeDamage(stats.damage);
            }

            if (!Application.isPlaying || !GameController.Instance.CanInteract) return;
            StopAllCoroutines();
            StartCoroutine(Pulse(radius));
        }

        internal override void StopEffects()
        {
            base.StopEffects();
            pulse.gameObject.SetActive(false);
        }

        private IEnumerator Pulse(float radius)
        {
            const float duration = 0.25f;
            Vector3 spriteSize = pulse.sprite.bounds.size;
            Vector3 fullScale = new(2f * radius / spriteSize.x, 2f * radius / spriteSize.y, 1f);
            pulse.gameObject.SetActive(true);
            for (float elapsed = 0f; elapsed < duration; elapsed += Time.deltaTime)
            {
                pulse.transform.localScale = Vector3.Lerp(Vector3.zero, fullScale, elapsed / duration);
                yield return null;
            }
            pulse.transform.localScale = fullScale;
            yield return null;
            pulse.gameObject.SetActive(false);
        }
    }
}
