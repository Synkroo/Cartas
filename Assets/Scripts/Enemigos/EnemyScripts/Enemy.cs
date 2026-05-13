using System;

[Serializable]
public class Enemy
{
    public EnemyData data;

    // SOLO estado runtime
    public int currentHealth;

    public void Initialize(EnemyData enemyData)
    {
        data = enemyData;

        currentHealth = data.maxHealth;
    }

    public bool IsDead()
    {
        return currentHealth <= 0;
    }
}