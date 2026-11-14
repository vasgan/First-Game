using System.Collections;using UnityEngine;
namespace FirstGame
{
    public sealed class BeamAttack:TowerAttackBehaviour
    {
        [SerializeField] LineRenderer beam;
        public override void Attack(Enemy target){if(!target)return;target.TakeDamage(Context.Tower.Stats.damage);StopAllCoroutines();StartCoroutine(Show(target.transform.position));}
        IEnumerator Show(Vector3 end){beam.SetPosition(0,Context.FirePoint.position);beam.SetPosition(1,end);beam.enabled=true;yield return new WaitForSeconds(.14f);beam.enabled=false;}
    }
}
