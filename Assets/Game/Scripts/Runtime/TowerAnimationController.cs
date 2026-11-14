using System.Collections;
using Spine.Unity;
using UnityEngine;

namespace FirstGame
{
    public sealed class TowerAnimationController : MonoBehaviour
    {
        [SerializeField] private Transform visual;
        [SerializeField] private Animator animator;
        [SerializeField] private SkeletonAnimation skeletonAnimation;
        private AttackAnimationStyle style;
        private Vector3 rest;
        private Vector3 baseScale;
        private bool facingLeft;

        public void Initialize(AttackAnimationStyle value)
        {
            style = value;
            baseScale = visual.localScale;
            rest = baseScale;
            if (skeletonAnimation)
            {
                skeletonAnimation.Initialize(false);
                skeletonAnimation.AnimationState.SetAnimation(0, IdleAnimation, true);
            }
        }

        private string IdleAnimation => facingLeft ? "idle_l_b" : "idle_r_b";

        public void SetLevel(int level)
        {
            StopAllCoroutines();
            rest = baseScale * Mathf.Pow(1.1f, Mathf.Max(0, level - 1));
            visual.localScale = rest;
        }

        public void PlayAttack(Enemy target, float attackInterval)
        {
            if (skeletonAnimation)
            {
                Vector3 direction = target ? target.transform.position - transform.position : Vector3.down;
                facingLeft = direction.x < 0f;
                string animation = "attack_" + (facingLeft ? "l" : "r") + (direction.y > 0f ? "_t" : "_b");
                Spine.TrackEntry entry = skeletonAnimation.AnimationState.SetAnimation(0, animation, false);
                // Fast towers finish the attack pose before the next shot without changing damage timing.
                entry.TimeScale = Mathf.Max(1f, entry.Animation.Duration / Mathf.Max(0.05f, attackInterval));
                skeletonAnimation.AnimationState.AddAnimation(0, IdleAnimation, true, 0f);
                return;
            }

            StopAllCoroutines();
            StartCoroutine(Animate());
            if (animator && animator.runtimeAnimatorController) animator.SetTrigger("Attack");
        }

        private IEnumerator Animate()
        {
            visual.localScale = style == AttackAnimationStyle.Recoil
                ? new Vector3(rest.x * 1.12f, rest.y * .82f, rest.z)
                : rest * (style == AttackAnimationStyle.Pulse ? 1.22f : 1.12f);
            yield return new WaitForSeconds(.1f);
            visual.localScale = rest;
        }
    }
}
