using UnityEngine;

namespace JuegoDeCartas.Managers
{
    [System.Serializable]
    public class Entity
    {
        public JuegoDeCartas.Stats.Stats stats = new JuegoDeCartas.Stats.Stats();

        public int TakeDamage(int damage)
        {
            int remaining = damage;

            if (stats.armor > 0)
            {
                int absorbed = Mathf.Min(stats.armor, remaining);
                stats.armor -= absorbed;
                remaining -= absorbed;
            }

            stats.health -= remaining;
            stats.Clamp();
            return remaining;
        }
    }
}
