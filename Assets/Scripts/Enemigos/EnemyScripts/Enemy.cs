using System;
using System.Collections.Generic;
using JuegoDeCartas.Managers;
using JuegoDeCartas.Missions;
using UnityEngine;

namespace JuegoDeCartas.Enemies
{
    [Serializable]
    public class Enemy
    {
        public EnemyData data;
        public Stats.Stats stats = new Stats.Stats();
        public int damageModifier;
        public int lastDamageTaken;
        public int turnsSurvived;
        public int currentMinDamage { get; private set; }
        public int currentMaxDamage { get; private set; }
        public int currentGoldRewardOverride { get; private set; }
        public Sprite currentSprite { get; private set; }
        public RuntimeAnimatorController currentAnimatorController { get; private set; }

        private BattleManager battle;
        private readonly List<int> mechanicUseCounts = new List<int>();
        private readonly List<int> mechanicDamageAccumulations = new List<int>();

        public void Initialize(EnemyData enemyData, BattleManager ownerBattle)
        {
            data = enemyData;
            battle = ownerBattle;

            float missionStatMultiplier = MissionRunState.EnemyStatMultiplier;

            stats.maxHealth = ApplyMultiplier(data.maxHealth, missionStatMultiplier, 1);
            stats.health = stats.maxHealth;
            stats.armor = ApplyMultiplier(data.startArmor, missionStatMultiplier, 0);

            currentMinDamage = ApplyMultiplier(data.minDamage, missionStatMultiplier, 0);
            currentMaxDamage = Mathf.Max(currentMinDamage, ApplyMultiplier(data.maxDamage, missionStatMultiplier, currentMinDamage));
            currentGoldRewardOverride = data.goldRewardOverride;
            currentSprite = data.sprite;
            currentAnimatorController = data.animatorController;

            damageModifier = 0;
            lastDamageTaken = 0;
            turnsSurvived = 0;

            mechanicUseCounts.Clear();
            mechanicDamageAccumulations.Clear();
            int mechanicCount = data.mechanics != null ? data.mechanics.Count : 0;
            for (int i = 0; i < mechanicCount; i++)
            {
                mechanicUseCounts.Add(0);
                mechanicDamageAccumulations.Add(0);
            }
        }

        public void BeginTurn()
        {
            turnsSurvived++;

            if (data == null)
                return;

            ExecuteTurnStartMechanics();
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

        public int RollProjectedNextAttackDamage()
        {
            int projectedModifier = GetProjectedDamageModifierForNextTurn();
            int minDamage = Mathf.Max(0, currentMinDamage + projectedModifier);
            int maxDamage = Mathf.Max(minDamage, currentMaxDamage + projectedModifier);
            return UnityEngine.Random.Range(minDamage, maxDamage + 1);
        }

        public int GetProjectedNextTurnArmorGain()
        {
            int armorGain = 0;
            foreach (var mechanic in GetStatsPerTurnMechanicsForNextTurn())
            {
                if (mechanic.armorPerTurn > 0)
                    armorGain += mechanic.armorPerTurn;
            }

            return armorGain;
        }

        public int GetProjectedNextTurnHeal()
        {
            int heal = 0;
            foreach (var mechanic in GetStatsPerTurnMechanicsForNextTurn())
            {
                if (mechanic.healPerTurn > 0)
                    heal += mechanic.healPerTurn;
            }

            return heal;
        }

        public int GetProjectedNextTurnDamageModifierGain()
        {
            int damageGain = 0;
            foreach (var mechanic in GetStatsPerTurnMechanicsForNextTurn())
            {
                if (mechanic.damageRampPerTurn > 0)
                    damageGain += mechanic.damageRampPerTurn;
            }

            return damageGain;
        }

        public bool TryHandleDefeat()
        {
            if (data == null || data.mechanics == null)
                return false;

            for (int i = 0; i < data.mechanics.Count; i++)
            {
                EnemyMechanicData mechanic = data.mechanics[i];
                if (mechanic == null || mechanic.mechanicType != EnemyMechanicType.ReviveOnDeath)
                    continue;

                int usedPhases = i < mechanicUseCounts.Count ? mechanicUseCounts[i] : 0;
                if (usedPhases >= Mathf.Max(0, mechanic.reviveCount))
                    continue;

                mechanicUseCounts[i] = usedPhases + 1;
                ApplyRevive(mechanic);
                return true;
            }

            return false;
        }

        void ExecuteTurnStartMechanics()
        {
            if (battle == null || battle.deckManager == null || data == null || data.mechanics == null)
                return;

            for (int i = 0; i < data.mechanics.Count; i++)
            {
                EnemyMechanicData mechanic = data.mechanics[i];
                if (mechanic == null)
                    continue;

                if (mechanic.mechanicType == EnemyMechanicType.StatsPerTurn)
                {
                    int statsFirstTriggerTurn = Mathf.Max(1, mechanic.firstTriggerTurn);
                    if (turnsSurvived < statsFirstTriggerTurn)
                        continue;

                    if (mechanic.healPerTurn > 0)
                        stats.health = Mathf.Min(stats.maxHealth, stats.health + mechanic.healPerTurn);

                    if (mechanic.armorPerTurn > 0)
                        stats.armor += mechanic.armorPerTurn;

                    if (mechanic.damageRampPerTurn > 0)
                    {
                        damageModifier += mechanic.damageRampPerTurn;
                        if (i < mechanicDamageAccumulations.Count)
                            mechanicDamageAccumulations[i] += mechanic.damageRampPerTurn;
                    }

                    continue;
                }

                if (mechanic.mechanicType != EnemyMechanicType.AddJunkToDeckOnTurnStart)
                    continue;

                if (mechanic.junkCard == null || mechanic.cardsToAdd <= 0)
                    continue;

                int firstTriggerTurn = Mathf.Max(1, mechanic.firstTriggerTurn);
                int triggerEveryXTurns = Mathf.Max(1, mechanic.triggerEveryXTurns);

                if (turnsSurvived < firstTriggerTurn)
                    continue;

                if ((turnsSurvived - firstTriggerTurn) % triggerEveryXTurns != 0)
                    continue;

                battle.deckManager.AddCard(
                    mechanic.junkCard,
                    mechanic.cardDestination,
                    mechanic.cardsToAdd,
                    mechanic.shuffleIntoDrawPile
                );
            }
        }

        int GetProjectedDamageModifierForNextTurn()
        {
            int projectedModifier = damageModifier;

            foreach (var mechanic in GetStatsPerTurnMechanicsForNextTurn())
            {
                if (mechanic.damageRampPerTurn != 0)
                    projectedModifier += mechanic.damageRampPerTurn;
            }

            return projectedModifier;
        }

        IEnumerable<EnemyMechanicData> GetStatsPerTurnMechanicsForNextTurn()
        {
            int nextTurnNumber = turnsSurvived + 1;

            if (data == null || data.mechanics == null)
                yield break;

            for (int i = 0; i < data.mechanics.Count; i++)
            {
                EnemyMechanicData mechanic = data.mechanics[i];
                if (mechanic == null || mechanic.mechanicType != EnemyMechanicType.StatsPerTurn)
                    continue;

                int firstTriggerTurn = Mathf.Max(1, mechanic.firstTriggerTurn);
                if (nextTurnNumber < firstTriggerTurn)
                    continue;

                yield return mechanic;
            }
        }

        public int GetMechanicCurrentDamageAccumulation(int mechanicIndex)
        {
            if (mechanicIndex < 0 || mechanicIndex >= mechanicDamageAccumulations.Count)
                return 0;

            return Mathf.Max(0, mechanicDamageAccumulations[mechanicIndex]);
        }

        public int GetRemainingRevivesForMechanic(int mechanicIndex)
        {
            if (data == null || data.mechanics == null || mechanicIndex < 0 || mechanicIndex >= data.mechanics.Count)
                return 0;

            EnemyMechanicData mechanic = data.mechanics[mechanicIndex];
            if (mechanic == null || mechanic.mechanicType != EnemyMechanicType.ReviveOnDeath)
                return 0;

            int used = mechanicIndex < mechanicUseCounts.Count ? mechanicUseCounts[mechanicIndex] : 0;
            return Mathf.Max(0, mechanic.reviveCount - used);
        }

        void ApplyRevive(EnemyMechanicData mechanic)
        {
            stats.maxHealth = ApplyPercent(stats.maxHealth, mechanic.maxHealthPercentOnRevive, 1);
            stats.health = stats.maxHealth;
            stats.armor = ApplyPercent(stats.armor, mechanic.armorPercentOnRevive, 0);
            currentMinDamage = ApplyPercent(currentMinDamage, mechanic.minDamagePercentOnRevive, 0);
            currentMaxDamage = Mathf.Max(currentMinDamage, ApplyPercent(currentMaxDamage, mechanic.maxDamagePercentOnRevive, currentMinDamage));

            damageModifier = 0;
            lastDamageTaken = 0;
            turnsSurvived = 0;
        }

        static int ApplyPercent(int value, float percent, int minimum)
        {
            return Mathf.Max(minimum, Mathf.RoundToInt(value * percent / 100f));
        }

        static int ApplyMultiplier(int value, float multiplier, int minimum)
        {
            return Mathf.Max(minimum, Mathf.RoundToInt(value * multiplier));
        }
    }
}
