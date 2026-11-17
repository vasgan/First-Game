using System.Collections.Generic;
using UnityEngine;

namespace FirstGame
{
    public sealed class ProjectileAttack : TowerAttackBehaviour
    {
        [SerializeField] private GameObject projectilePrefab;
        private readonly Queue<Projectile> pool = new();

        internal override bool HasRequiredReferences => projectilePrefab && projectilePrefab.GetComponent<Projectile>();

        public override void Attack(Enemy target)
        {
            if (!target || !target.IsAlive) return;

            Projectile projectile = pool.Count > 0
                ? pool.Dequeue()
                : Instantiate(projectilePrefab).GetComponent<Projectile>();
            TowerLevelConfig stats = Context.Tower.Stats;
            projectile.Launch(Context.FirePoint.position, target, stats.damage, stats.projectileSpeed, Return,
                Context.Tower.Config.chillsTarget);
        }

        private void Return(Projectile projectile) => pool.Enqueue(projectile);
    }
}
