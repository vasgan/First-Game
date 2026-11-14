using UnityEngine;
namespace FirstGame
{
    public sealed class TowerAttackContext
    {
        public TowerController Tower { get; }
        public Transform FirePoint { get; }
        public TowerAnimationController Animation { get; }
        public TowerAttackContext(TowerController tower, Transform firePoint, TowerAnimationController animation) { Tower=tower; FirePoint=firePoint; Animation=animation; }
    }
    public abstract class TowerAttackBehaviour : MonoBehaviour, ITowerAttack
    {
        protected TowerAttackContext Context { get; private set; }
        public virtual void Initialize(TowerAttackContext context) => Context=context;
        public abstract void Attack(Enemy target);
    }
}
