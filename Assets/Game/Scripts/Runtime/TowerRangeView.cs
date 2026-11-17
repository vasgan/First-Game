using UnityEngine;
namespace FirstGame
{
    public sealed class TowerRangeView:MonoBehaviour
    {
        [SerializeField] SpriteRenderer attackRange; [SerializeField] SpriteRenderer effectRange;
        public float AttackRadius { get; private set; }
        public float EffectRadius { get; private set; }
        internal bool HasRequiredReferences => attackRange && effectRange;
        public void Initialize(Color c){attackRange.color=new Color(c.r,c.g,c.b,Mathf.Max(c.a,.28f));effectRange.color=new Color(c.r,c.g,c.b,Mathf.Max(c.a,.42f));Hide();}
        public void Show(float range,float effect){AttackRadius=Mathf.Max(0f,range);EffectRadius=Mathf.Max(0f,effect);attackRange.transform.localScale=Vector3.one*DisplayedDiameter(AttackRadius);effectRange.transform.localScale=Vector3.one*DisplayedDiameter(EffectRadius);attackRange.gameObject.SetActive(true);effectRange.gameObject.SetActive(EffectRadius>.01f);}
        public void Hide(){attackRange.gameObject.SetActive(false);effectRange.gameObject.SetActive(false);}
        public static float DisplayedDiameter(float radius) => Mathf.Max(0f, radius) * 2f;
    }
}
