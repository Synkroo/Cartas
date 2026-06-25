using UnityEngine;

namespace JuegoDeCartas.Managers
{
    [System.Serializable]
    public class Entity
    {
        public JuegoDeCartas.Stats.Stats stats = new JuegoDeCartas.Stats.Stats();

        public DamageResult TakeDamage(int damage)
        {
            int attempted = Mathf.Max(0, damage);
            int remaining = attempted;
            int absorbed = 0;

            if (stats.armor > 0)
            {
                absorbed = Mathf.Min(stats.armor, remaining);
                stats.armor -= absorbed;
                remaining -= absorbed;
            }

            int previousHealth = Mathf.Max(0, stats.health);
            int healthDamage = Mathf.Min(previousHealth, remaining);
            stats.health -= healthDamage;
            stats.Clamp();
            return new DamageResult(
                attempted,
                absorbed,
                healthDamage,
                previousHealth > 0 && stats.health <= 0
            );
        }
    }
}
