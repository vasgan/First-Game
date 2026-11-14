using UnityEngine;
namespace FirstGame
{
    public sealed class TowerRangeView:MonoBehaviour
    {
        [SerializeField] SpriteRenderer attackRange; [SerializeField] SpriteRenderer effectRange;
        public void Initialize(Color c){attackRange.color=c;effectRange.color=new Color(c.r,c.g,c.b,c.a*.75f);Hide();}
        public void Show(float range,float effect){attackRange.transform.localScale=Vector3.one*range*2;effectRange.transform.localScale=Vector3.one*effect*2;attackRange.gameObject.SetActive(true);effectRange.gameObject.SetActive(effect>.01f);}
        public void Hide(){attackRange.gameObject.SetActive(false);effectRange.gameObject.SetActive(false);}
    }
}
