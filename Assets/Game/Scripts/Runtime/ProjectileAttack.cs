using System.Collections.Generic;using UnityEngine;
namespace FirstGame
{
    public sealed class ProjectileAttack:TowerAttackBehaviour
    {
        [SerializeField] GameObject projectilePrefab;readonly Queue<Projectile> pool=new();
        public override void Attack(Enemy target){if(!target)return;Projectile p=pool.Count>0?pool.Dequeue():Instantiate(projectilePrefab).GetComponent<Projectile>();p.Launch(Context.FirePoint.position,target,Context.Tower.Stats.damage,Context.Tower.Stats.projectileSpeed,Return);}
        void Return(Projectile p){p.gameObject.SetActive(false);pool.Enqueue(p);}
    }
}
