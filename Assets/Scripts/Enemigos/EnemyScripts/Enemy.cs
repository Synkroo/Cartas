using System;

namespace JuegoDeCartas.Enemies
{
    [Serializable]
    public class Enemy
    {
        public EnemyData data;

        public int currentHealth;
        public int maxHealth;
        public int damageModifier;

        public void Initialize(EnemyData enemyData)
        {
            data = enemyData;

            maxHealth = data.maxHealth;
            currentHealth = maxHealth;
            damageModifier = 0;
        }

    }
}
