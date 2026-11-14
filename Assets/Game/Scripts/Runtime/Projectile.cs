using UnityEngine;

namespace FirstGame
{
    public sealed class Projectile : MonoBehaviour
    {
        private Enemy target;
        private int damage;
        private float speed;
        private System.Action<Projectile> returned;
        public bool InUse { get; private set; }
        public void Launch(Vector3 origin, Enemy newTarget, int newDamage, float newSpeed,
            System.Action<Projectile> onReturned)
        {
            transform.position = origin; target = newTarget; damage = newDamage;
            speed = newSpeed; returned = onReturned; InUse = true; gameObject.SetActive(true);
        }
        private void Update()
        {
            if (!InUse) return;
            if (!target || !target.IsAlive) { Return(); return; }
            transform.position = Vector3.MoveTowards(transform.position, target.transform.position, speed * Time.deltaTime);
            if ((transform.position - target.transform.position).sqrMagnitude > 0.01f) return;
            target.TakeDamage(damage); Return();
        }
        private void Return(){InUse=false;returned?.Invoke(this);}
    }
}
