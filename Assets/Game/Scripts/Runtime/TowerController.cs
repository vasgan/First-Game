using System.Collections;using UnityEngine;
namespace FirstGame
{
    public sealed class TowerController:MonoBehaviour
    {
        [SerializeField] SpriteRenderer body;[SerializeField] Transform firePoint;[SerializeField] TowerTargeting targeting;[SerializeField] TowerRangeView rangeView;[SerializeField] TowerAnimationController animationController;[SerializeField] TowerAttackBehaviour attackBehaviour;
        TowerConfig config;float cooldown;public int LevelIndex{get;private set;}public int Level=>LevelIndex+1;public TowerConfig Config=>config;public TowerLevelConfig Stats=>config.levels[LevelIndex];public bool CanUpgrade=>LevelIndex+1<config.levels.Length;
        public void Initialize(TowerConfig value){config=value;LevelIndex=0;animationController.Initialize(config.animationStyle);ApplyLevel();rangeView.Initialize(config.rangeColor);attackBehaviour.Initialize(new TowerAttackContext(this,firePoint,animationController));}
        void Update(){if(!config||GameController.Instance.State!=GameState.Wave)return;cooldown-=Time.deltaTime;if(cooldown>0)return;Enemy target=targeting.FindTarget(transform.position,Stats.range,config.targetingRule);if(!target&&attackBehaviour is not AreaAttack)return;StartCoroutine(AttackAfterDelay(target));cooldown=1f/Mathf.Max(.05f,Stats.attackRate);}
        IEnumerator AttackAfterDelay(Enemy target){animationController.PlayAttack(target,1f/Mathf.Max(.05f,Stats.attackRate));if(Stats.damageDelay>0)yield return new WaitForSeconds(Stats.damageDelay);attackBehaviour.Attack(target);}
        void ApplyLevel(){if(body&&Stats.sprite)body.sprite=Stats.sprite;animationController.SetLevel(Level);}
        public bool Upgrade(){if(!CanUpgrade||!GameController.Instance.Economy.Spend(Stats.upgradeCost))return false;LevelIndex++;ApplyLevel();rangeView.Show(Stats.range,Stats.effectRadius);return true;}
        public void SetSelected(bool value){if(value)rangeView.Show(Stats.range,Stats.effectRadius);else rangeView.Hide();}
        void OnMouseDown(){if(GameController.Instance.CanInteract)GameController.Instance.UI.SelectTower(this);}
    }
}
