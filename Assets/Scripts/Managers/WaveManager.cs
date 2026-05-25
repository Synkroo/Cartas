using System;
using System.Collections.Generic;
using UnityEngine;
using JuegoDeCartas.Enemies;
using JuegoDeCartas.Stats;
using JuegoDeCartas.UI;

namespace JuegoDeCartas.Managers
{
    [System.Serializable]
    public class WaveManager
    {
        [Header("Enemy Wave")]
        public List<EnemyData> enemyWave = new List<EnemyData>();
        private int currentEnemyIndex = 0;

        public Enemy enemy { get; private set; }

        public UIManager uiManager;
        public GameManager gameManager;
        public GameStatsTracker statsTracker;
        public EnemyHealthBar enemyHealthBar;

        public event Action OnEnemyDefeated;
        public event Action OnWaveCleared;
        public event Action<int> OnGoldEarned;

        public void Initialize()
        {
            Shuffle();
            SpawnNext();
        }

        void Shuffle()
        {
            for (int i = 0; i < enemyWave.Count; i++)
            {
                EnemyData temp = enemyWave[i];
                int randomIndex = UnityEngine.Random.Range(i, enemyWave.Count);
                enemyWave[i] = enemyWave[randomIndex];
                enemyWave[randomIndex] = temp;
            }
        }

        public void SpawnNext()
        {
            if (currentEnemyIndex >= enemyWave.Count)
            {
                enemy = null;
                if (enemyHealthBar != null)
                    enemyHealthBar.enabled = false;
                return;
            }

            enemy = new Enemy();
            enemy.Initialize(enemyWave[currentEnemyIndex]);
            currentEnemyIndex++;

            if (uiManager != null)
                uiManager.SetEnemySprite(enemy.data.sprite);

            if (enemyHealthBar != null)
                enemyHealthBar.enabled = true;
        }

        public void DamageEnemy(int damage, out bool died)
        {
            died = false;
            if (enemy == null) return;

            died = enemy.TakeDamage(damage);

            if (died)
            {
                enemy.stats.health = 0;
                HandleDeath();
            }
        }

        void HandleDeath()
        {
            if (statsTracker != null)
                statsTracker.RegisterEnemyDefeated();

            int gold = (enemy != null && enemy.data != null) ? (enemy.data.isBoss ? 500 : 200) : 0;

            if (gold > 0 && gameManager != null)
                gameManager.dinero += gold;

            OnGoldEarned?.Invoke(gold);
            OnEnemyDefeated?.Invoke();

            bool noMoreEnemies = currentEnemyIndex >= enemyWave.Count;

            if (noMoreEnemies)
            {
                if (enemyHealthBar != null)
                    enemyHealthBar.enabled = false;

                if (statsTracker != null)
                    statsTracker.PopulateStatsText();

                OnWaveCleared?.Invoke();
                return;
            }

            if (gameManager != null)
                gameManager.OpenShop();
        }

        public bool HasMoreEnemies()
        {
            return currentEnemyIndex < enemyWave.Count;
        }
    }
}
