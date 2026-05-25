using System;
using UnityEngine;
using JuegoDeCartas.Stats;

namespace JuegoDeCartas.Enemies
{
    [Serializable]
    public class Enemy
    {
        public EnemyData data;
        public Stats.Stats stats = new Stats.Stats();
        public int damageModifier;
        public int lastDamageTaken;

        public void Initialize(EnemyData enemyData)
        {
            data = enemyData;
            stats.maxHealth = data.maxHealth;
            stats.health = stats.maxHealth;
            damageModifier = 0;
            lastDamageTaken = 0;
        }

        public bool TakeDamage(int damage)
        {
            int remaining = damage;
            if (stats.armor > 0)
            {
                int absorbed = Mathf.Min(stats.armor, remaining);
                stats.armor -= absorbed;
                remaining -= absorbed;
            }
            stats.health -= remaining;
            lastDamageTaken += damage;
            return stats.health <= 0;
        }
    }
}
