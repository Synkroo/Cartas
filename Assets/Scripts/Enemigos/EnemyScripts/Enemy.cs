using System;
using JuegoDeCartas.Stats;

namespace JuegoDeCartas.Enemies
{
    [Serializable]
    public class Enemy
    {
        public EnemyData data;
        public Stats.Stats stats = new Stats.Stats();
        public int damageModifier;

        public void Initialize(EnemyData enemyData)
        {
            data = enemyData;

            stats.maxHealth = data.maxHealth;
            stats.health = stats.maxHealth;
            damageModifier = 0;
        }

    }
}
