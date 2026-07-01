using System;
using System.Collections.Generic;
using JuegoDeCartas.Managers;
using JuegoDeCartas.Missions;
using JuegoDeCartas.Challenges;
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
        public bool IsDefeated { get; private set; }
        public int weaknessStacks { get; private set; }
        public int poisonStacks { get; private set; }
        public int bleedStacks { get; private set; }
        public int stunStacks { get; private set; }

        private BattleManager battle;
        private bool weaknessAppliedSinceLastTick;
        private bool bleedAppliedSinceLastTick;
        private readonly List<int> mechanicUseCounts = new List<int>();
        private readonly List<int> mechanicDamageAccumulations = new List<int>();

        public void Initialize(EnemyData enemyData, BattleManager ownerBattle)
        {
            data = enemyData;
            battle = ownerBattle;

            if (data == null)
            {
                stats.maxHealth = 1;
                stats.health = 1;
                stats.armor = 0;
                currentMinDamage = 0;
                currentMaxDamage = 0;
                currentGoldRewardOverride = 0;
                currentSprite = null;
                currentAnimatorController = null;
                damageModifier = 0;
                lastDamageTaken = 0;
                turnsSurvived = 0;
                mechanicUseCounts.Clear();
                mechanicDamageAccumulations.Clear();
                ClearStatuses();
                IsDefeated = false;
                return;
            }

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
            ClearStatuses();
            IsDefeated = false;
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

        public DamageResult TakeDamage(int damage)
        {
            int attempted = Mathf.Max(0, damage);
            if (IsDefeated || stats.health <= 0)
                return DamageResult.Ignored(attempted);

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
            lastDamageTaken += healthDamage;
            IsDefeated = previousHealth > 0 && stats.health <= 0;
            return new DamageResult(
                attempted,
                absorbed,
                healthDamage,
                IsDefeated
            );
        }

        public int RollProjectedNextAttackDamage()
        {
            int projectedModifier = GetProjectedDamageModifierForNextTurn();
            int minDamage = Mathf.Max(0, currentMinDamage + projectedModifier);
            int maxDamage = Mathf.Max(minDamage, currentMaxDamage + projectedModifier);
            return RunRandom.Range(minDamage, maxDamage + 1);
        }

        public void AddStatus(EnemyStatusType statusType, int amount)
        {
            if (amount <= 0)
                return;

            switch (statusType)
            {
                case EnemyStatusType.Weakness:
                    weaknessStacks += amount;
                    weaknessAppliedSinceLastTick = true;
                    break;
                case EnemyStatusType.Poison:
                    poisonStacks = Mathf.Max(poisonStacks, 1);
                    break;
                case EnemyStatusType.Bleed:
                    bleedStacks += amount;
                    bleedAppliedSinceLastTick = true;
                    break;
                case EnemyStatusType.Stun:
                    stunStacks += amount;
                    break;
            }
        }

        public int GetStatus(EnemyStatusType statusType)
        {
            return statusType switch
            {
                EnemyStatusType.Weakness => weaknessStacks,
                EnemyStatusType.Poison => poisonStacks,
                EnemyStatusType.Bleed => bleedStacks,
                EnemyStatusType.Stun => stunStacks,
                _ => 0
            };
        }

        public int ConsumeStatus(EnemyStatusType statusType, int amount)
        {
            if (amount <= 0)
                return 0;

            int current = GetStatus(statusType);
            int consumed = Mathf.Min(current, amount);
            SetStatus(statusType, current - consumed);
            return consumed;
        }

        public void SetStatus(EnemyStatusType statusType, int amount)
        {
            int value = Mathf.Max(0, amount);
            switch (statusType)
            {
                case EnemyStatusType.Weakness:
                    weaknessStacks = value;
                    break;
                case EnemyStatusType.Poison:
                    poisonStacks = value;
                    break;
                case EnemyStatusType.Bleed:
                    bleedStacks = value;
                    break;
                case EnemyStatusType.Stun:
                    stunStacks = value;
                    break;
            }
        }

        public bool WasStatusAppliedSinceLastTick(EnemyStatusType statusType)
        {
            return statusType switch
            {
                EnemyStatusType.Weakness => weaknessAppliedSinceLastTick,
                EnemyStatusType.Bleed => bleedAppliedSinceLastTick,
                _ => false
            };
        }

        public void ClearStatusAppliedSinceLastTick(EnemyStatusType statusType)
        {
            switch (statusType)
            {
                case EnemyStatusType.Weakness:
                    weaknessAppliedSinceLastTick = false;
                    break;
                case EnemyStatusType.Bleed:
                    bleedAppliedSinceLastTick = false;
                    break;
            }
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
            stats.Clamp();

            damageModifier = 0;
            lastDamageTaken = 0;
            turnsSurvived = 0;
            ClearStatuses();
            IsDefeated = false;
        }

        void ClearStatuses()
        {
            weaknessStacks = 0;
            poisonStacks = 0;
            bleedStacks = 0;
            stunStacks = 0;
            weaknessAppliedSinceLastTick = false;
            bleedAppliedSinceLastTick = false;
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
