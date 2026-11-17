using System.Collections;
using UnityEngine;

namespace FirstGame
{
    public sealed class BeamAttack : TowerAttackBehaviour
    {
        [SerializeField] LineRenderer beam;
        internal override bool HasRequiredReferences => beam;
        public override void Attack(Enemy target)
        {
            if (!target || !target.IsAlive) return;
            Vector3 impact = target.transform.position;
            target.TakeDamage(Context.Tower.Stats.damage);
            if (!Application.isPlaying || !GameController.Instance.CanInteract) return;
            StopAllCoroutines();
            StartCoroutine(Show(impact));
        }

        internal override void StopEffects()
        {
            base.StopEffects();
            beam.enabled = false;
        }

        private IEnumerator Show(Vector3 end)
        {
            beam.SetPosition(0, Context.FirePoint.position);
            beam.SetPosition(1, end);
            beam.enabled = true;
            yield return new WaitForSeconds(0.14f);
            beam.enabled = false;
        }
    }
}
