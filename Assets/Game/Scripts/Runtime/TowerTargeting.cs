using UnityEngine;
namespace FirstGame
{
    public sealed class TowerTargeting : MonoBehaviour
    {
        public Enemy FindTarget(Vector3 origin,float range,TargetingRule rule)
        {
            Enemy best=null; float scoreBest=float.MinValue; float limit=range*range;
            foreach(Enemy enemy in Enemy.Active){if(!enemy||!enemy.IsAlive)continue;float d=(enemy.transform.position-origin).sqrMagnitude;if(d>limit)continue;float s=rule switch{TargetingRule.ClosestToBase=>-enemy.RemainingRouteDistance,TargetingRule.Nearest=>-d,TargetingRule.Strongest=>enemy.Health,_=>float.MinValue};if(s>scoreBest){best=enemy;scoreBest=s;}}
            return best;
        }
    }
}
