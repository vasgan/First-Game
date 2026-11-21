using System.Collections;using UnityEngine;
namespace FirstGame
{
    public sealed class TowerController:MonoBehaviour
    {
        [SerializeField] SpriteRenderer body;[SerializeField] Transform firePoint;[SerializeField] TowerTargeting targeting;[SerializeField] TowerRangeView rangeView;[SerializeField] TowerAnimationController animationController;[SerializeField] TowerAttackBehaviour attackBehaviour;
        internal bool HasRequiredReferences => firePoint && targeting && rangeView && rangeView.HasRequiredReferences &&
            animationController && animationController.HasRequiredReferences &&
            attackBehaviour && attackBehaviour.HasRequiredReferences;
        TowerConfig config;BuildSpot buildSpot;TargetingRule targetingRule;TowerSpecializationConfig specialization;float cooldown;public int LevelIndex{get;private set;}public int Level=>LevelIndex+1;public TowerConfig Config=>config;public TowerLevelConfig Stats=>specialization != null ? specialization.stats : config.levels[LevelIndex];public bool CanUpgrade=>specialization == null && LevelIndex+1<config.levels.Length;public bool CanChooseSpecialization => specialization == null && LevelIndex == 0 && config != null && config.specializations != null && config.specializations.Length > 0;public TowerSpecializationConfig Specialization => specialization;public TargetingRule TargetingRule=>targetingRule;
        public void Initialize(TowerConfig value){config=value;targetingRule=config.targetingRule;LevelIndex=0;animationController.Initialize(config.animationStyle);ApplyLevel();rangeView.Initialize(config.rangeColor);attackBehaviour.Initialize(new TowerAttackContext(this,firePoint,animationController));}
        internal void SetBuildSpot(BuildSpot value) => buildSpot = value;
        public int SellValue
        {
            get
            {
                int invested = config ? config.buildCost : 0;
                for (int i = 0; config && i < LevelIndex; i++) invested += config.levels[i].upgradeCost;
                if (specialization != null) invested += specialization.cost;
                return Mathf.FloorToInt(invested * .7f);
            }
        }
        public bool Sell() => buildSpot && buildSpot.Sell(this);
        public void CycleTargetingRule() => targetingRule = (TargetingRule)(((int)targetingRule + 1) % 3);
        void Update(){if(!config||GameController.Instance.State!=GameState.Wave)return;cooldown-=Time.deltaTime;if(cooldown>0)return;Enemy target=targeting.FindTarget(transform.position,Stats.range,targetingRule);if(!target)return;StartCoroutine(AttackAfterDelay(target));cooldown=1f/Mathf.Max(.05f,Stats.attackRate);}
        IEnumerator AttackAfterDelay(Enemy target)
        {
            int spawnVersion = target.SpawnVersion;
            animationController.PlayAttack(target, 1f / Mathf.Max(.05f, Stats.attackRate));
            GameAudio.PlayAttack();
            if (Stats.damageDelay > 0) yield return new WaitForSeconds(Stats.damageDelay);
            if (!GameController.Instance.CanInteract) yield break;
            // The explosion uses live positions at impact, independently of the original target.
            if (attackBehaviour is AreaAttack || (target && target.IsAlive && target.SpawnVersion == spawnVersion))
                attackBehaviour.Attack(target);
        }

        internal void StopAttacking()
        {
            StopAllCoroutines();
            if (attackBehaviour) attackBehaviour.StopEffects();
        }
        void ApplyLevel(){if(body&&Stats.sprite)body.sprite=Stats.sprite;animationController.SetLevel(Level);}
        public bool Upgrade(){if(!CanUpgrade||!GameController.Instance.Economy.Spend(Stats.upgradeCost))return false;LevelIndex++;ApplyLevel();rangeView.Show(Stats.range,Stats.effectRadius);GameAudio.PlayUpgrade();return true;}
        public bool ChooseSpecialization(int index)
        {
            if (!CanChooseSpecialization || index < 0 || index >= config.specializations.Length) return false;
            TowerSpecializationConfig choice = config.specializations[index];
            if (choice == null || choice.stats == null || choice.cost < 0 || !GameController.Instance.Economy.Spend(choice.cost)) return false;
            specialization = choice;
            ApplyLevel();
            rangeView.Show(Stats.range, Stats.effectRadius);
            GameAudio.PlayUpgrade();
            return true;
        }
        public void SetSelected(bool value){if(value)rangeView.Show(Stats.range,Stats.effectRadius);else rangeView.Hide();}
        void OnMouseDown()
        {
            if (!UiInputGuard.IsPointerOverUi() && GameController.Instance.CanInteract)
                GameController.Instance.UI.SelectTower(this);
        }
    }
}
