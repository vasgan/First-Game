using System.Collections;using UnityEngine;
namespace FirstGame
{
    public sealed class AreaAttack:TowerAttackBehaviour
    {
        [SerializeField] SpriteRenderer pulse;
        public override void Attack(Enemy ignored){float radius=Context.Tower.Stats.effectRadius;foreach(Enemy e in Enemy.Active.ToArray())if(e&&e.IsAlive&&(e.transform.position-Context.Tower.transform.position).sqrMagnitude<=radius*radius)e.TakeDamage(Context.Tower.Stats.damage);StopAllCoroutines();StartCoroutine(Pulse(radius));}
        IEnumerator Pulse(float radius){pulse.gameObject.SetActive(true);pulse.transform.localScale=Vector3.zero;float t=0;while(t<.25f){t+=Time.deltaTime;pulse.transform.localScale=Vector3.one*radius*2*(t/.25f);yield return null;}pulse.gameObject.SetActive(false);}
    }
}
