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
        public List<EnemyData> normalEnemies = new List<EnemyData>();
        public List<EnemyData> miniBossEnemies = new List<EnemyData>();
        public EnemyData finalBoss;
        [Min(1)] public int totalCombats = 10;
        [Min(1)] public int miniBossFrequency = 3;

        readonly List<EnemyData> runtimeWave = new List<EnemyData>();
        private int currentEnemyIndex = 0;

        public Enemy enemy { get; private set; }

        public UIManager uiManager;
        public GameManager gameManager;
        public GameStatsTracker statsTracker;
        public EnemyHealthBar enemyHealthBar;
        public BattleManager battleManager;

        public event Action OnEnemyDefeated;
        public event Action OnWaveCleared;
        public event Action<int> OnGoldEarned;

        public void Initialize()
        {
            currentEnemyIndex = 0;
            BuildRuntimeWave();
            SpawnNext();
        }

        void BuildRuntimeWave()
        {
            runtimeWave.Clear();

            if (HasGeneratedRunConfiguration())
            {
                GenerateRunWave();
                return;
            }

            for (int i = 0; i < enemyWave.Count; i++)
            {
                if (enemyWave[i] != null)
                    runtimeWave.Add(enemyWave[i]);
            }

            Shuffle(runtimeWave);
        }

        bool HasGeneratedRunConfiguration()
        {
            return totalCombats > 0 &&
                   finalBoss != null &&
                   CountValidEnemies(normalEnemies) > 0;
        }

        void GenerateRunWave()
        {
            int lastCombatIndex = Mathf.Max(1, totalCombats);

            for (int combatNumber = 1; combatNumber <= lastCombatIndex; combatNumber++)
            {
                if (combatNumber == lastCombatIndex)
                {
                    runtimeWave.Add(finalBoss);
                    continue;
                }

                bool shouldSpawnMiniBoss =
                    miniBossFrequency > 0 &&
                    combatNumber % miniBossFrequency == 0 &&
                    CountValidEnemies(miniBossEnemies) > 0;

                EnemyData nextEnemy = shouldSpawnMiniBoss
                    ? GetRandomEnemy(miniBossEnemies)
                    : GetRandomEnemy(normalEnemies);

                if (nextEnemy == null && shouldSpawnMiniBoss)
                    nextEnemy = GetRandomEnemy(normalEnemies);

                if (nextEnemy != null)
                    runtimeWave.Add(nextEnemy);
            }
        }

        public void SpawnNext()
        {
            if (currentEnemyIndex >= runtimeWave.Count)
            {
                enemy = null;
                if (enemyHealthBar != null)
                    enemyHealthBar.enabled = false;
                return;
            }

            enemy = new Enemy();
            enemy.Initialize(runtimeWave[currentEnemyIndex], battleManager);
            currentEnemyIndex++;

            if (uiManager != null)
                uiManager.SetEnemySprite(enemy.currentSprite);

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
                if (enemy.TryHandleDefeat())
                {
                    died = false;

                    if (uiManager != null)
                        uiManager.SetEnemySprite(enemy.currentSprite);

                    if (enemyHealthBar != null)
                        enemyHealthBar.enabled = true;

                    return;
                }

                enemy.stats.health = 0;
                HandleDeath();
            }
        }

        void HandleDeath()
        {
            if (statsTracker != null)
                statsTracker.RegisterEnemyDefeated();

            int gold = 0;
            if (enemy != null && enemy.data != null)
            {
                if (enemy.currentGoldRewardOverride > 0)
                    gold = enemy.currentGoldRewardOverride;
                else if (enemy.data.IsBoss)
                    gold = 350;
                else if (enemy.data.IsMiniBoss)
                    gold = 170;
                else
                    gold = 90;
            }

            if (gold > 0 && gameManager != null)
                gameManager.dinero += gold;

            OnGoldEarned?.Invoke(gold);
            OnEnemyDefeated?.Invoke();

            bool noMoreEnemies = currentEnemyIndex >= runtimeWave.Count;

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
            return currentEnemyIndex < runtimeWave.Count;
        }

        static int CountValidEnemies(List<EnemyData> enemies)
        {
            int count = 0;

            for (int i = 0; i < enemies.Count; i++)
            {
                if (enemies[i] != null)
                    count++;
            }

            return count;
        }

        static EnemyData GetRandomEnemy(List<EnemyData> enemies)
        {
            List<EnemyData> candidates = new List<EnemyData>();
            for (int i = 0; i < enemies.Count; i++)
            {
                if (enemies[i] != null)
                    candidates.Add(enemies[i]);
            }

            if (candidates.Count == 0)
                return null;

            int randomIndex = UnityEngine.Random.Range(0, candidates.Count);
            return candidates[randomIndex];
        }

        static void Shuffle(List<EnemyData> wave)
        {
            for (int i = 0; i < wave.Count; i++)
            {
                EnemyData temp = wave[i];
                int randomIndex = UnityEngine.Random.Range(i, wave.Count);
                wave[i] = wave[randomIndex];
                wave[randomIndex] = temp;
            }
        }
    }
}
