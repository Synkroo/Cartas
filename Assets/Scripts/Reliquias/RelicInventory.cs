using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;
using JuegoDeCartas.Cards;
using JuegoDeCartas.Managers;
using JuegoDeCartas.UI;

namespace JuegoDeCartas.Relics
{
    public class RelicInventory : MonoBehaviour
    {
        public const int MaxRelics = 6;

        [Header("References")]
        public BattleManager battle;
        public RelicInventoryUI inventoryUI;

        [Header("Debug / Starting Relics")]
        public List<RelicData> startingRelics = new List<RelicData>();

        readonly List<RelicData> ownedRelics = new List<RelicData>();
        int cardsPlayedThisCombat;
        bool firstHitReductionAvailable;

        public event Action Changed;
        public IReadOnlyList<RelicData> OwnedRelics =>
            new ReadOnlyCollection<RelicData>(ownedRelics);
        public bool IsFull => ownedRelics.Count >= MaxRelics;

        public void Initialize(BattleManager battleManager)
        {
            battle = battleManager;
            ownedRelics.Clear();

            for (int i = 0;
                 i < startingRelics.Count && ownedRelics.Count < MaxRelics;
                 i++)
            {
                RelicData relic = startingRelics[i];
                if (relic != null && !ownedRelics.Contains(relic))
                    ownedRelics.Add(relic);
            }

            ResetCombatState();
            RefreshUI();
        }

        public bool CanAdd(RelicData relic)
        {
            return relic != null &&
                   !IsFull &&
                   !ownedRelics.Contains(relic);
        }

        public bool TryAdd(RelicData relic)
        {
            if (!CanAdd(relic))
                return false;

            ownedRelics.Add(relic);
            RefreshUI();
            Changed?.Invoke();
            return true;
        }

        public bool TryReplace(RelicData currentRelic, RelicData newRelic)
        {
            if (currentRelic == null ||
                newRelic == null ||
                currentRelic == newRelic ||
                ownedRelics.Contains(newRelic))
            {
                return false;
            }

            int index = ownedRelics.IndexOf(currentRelic);
            if (index < 0)
                return false;

            ownedRelics[index] = newRelic;
            RefreshUI();
            Changed?.Invoke();
            return true;
        }

        public bool Contains(RelicData relic)
        {
            return relic != null && ownedRelics.Contains(relic);
        }

        public int ModifyBuffDuration(int baseTurns)
        {
            return Mathf.Max(
                0,
                baseTurns + GetTotalAmount(RelicEffectType.BuffDuration)
            );
        }

        public int GetInterestBonus()
        {
            return GetTotalAmount(RelicEffectType.FlatInterest);
        }

        public int GetFirstCardDamageBonus(int cardsPlayedThisTurn)
        {
            return cardsPlayedThisTurn == 0
                ? GetTotalAmount(RelicEffectType.FirstCardDamage)
                : 0;
        }

        public int ModifyIncomingDamage(int damage)
        {
            int result = Mathf.Max(0, damage);
            if (!firstHitReductionAvailable)
                return result;

            int reduction = GetTotalAmount(RelicEffectType.ReduceFirstHit);
            if (reduction <= 0)
                return result;

            firstHitReductionAvailable = false;
            return Mathf.Max(0, result - reduction);
        }

        public void OnCombatStarted()
        {
            ResetCombatState();

            int armor = GetTotalAmount(RelicEffectType.CombatStartArmor);
            if (armor > 0 && battle != null)
                battle.GainPlayerArmor(armor);
        }

        public void OnPlayerTurnStarted()
        {
            int armor = GetTotalAmount(RelicEffectType.TurnStartArmor);
            if (armor > 0 && battle != null)
                battle.GainPlayerArmor(armor);
        }

        public void OnDamageDealt(int damage)
        {
            if (damage <= 0 || battle == null)
                return;

            int percentage = GetTotalPercentage(RelicEffectType.LifeSteal);
            int healing = Mathf.FloorToInt(damage * percentage / 100f);
            if (healing > 0)
                battle.HealPlayer(healing);
        }

        public void OnCardResolved(Card card, int paidCost)
        {
            cardsPlayedThisCombat++;

            if (paidCost == 0)
            {
                int armor = GetTotalAmount(
                    RelicEffectType.ZeroCostCardArmor
                );
                if (armor > 0 && battle != null)
                    battle.GainPlayerArmor(armor);
            }

            TriggerCardCounterEffect(
                RelicEffectType.DrawEveryCards,
                amount => battle?.DrawCards(amount)
            );
            TriggerCardCounterEffect(
                RelicEffectType.RestoreManaEveryCards,
                amount => battle?.RestorePlayerMana(amount)
            );
        }

        public int OnEnemyDefeated()
        {
            int healing = GetTotalAmount(RelicEffectType.HealAfterCombat);
            if (healing > 0 && battle != null)
                battle.HealPlayer(healing);

            return GetTotalAmount(RelicEffectType.GoldPerEnemy);
        }

        public int GetTotalAmount(RelicEffectType effectType)
        {
            int total = 0;
            for (int i = 0; i < ownedRelics.Count; i++)
            {
                RelicData relic = ownedRelics[i];
                if (relic != null && relic.effectType == effectType)
                    total += Mathf.Max(0, relic.amount);
            }

            return total;
        }

        public int GetTotalPercentage(RelicEffectType effectType)
        {
            int total = 0;
            for (int i = 0; i < ownedRelics.Count; i++)
            {
                RelicData relic = ownedRelics[i];
                if (relic != null && relic.effectType == effectType)
                    total += Mathf.Max(0, relic.percentage);
            }

            return Mathf.Clamp(total, 0, 100);
        }

        void TriggerCardCounterEffect(
            RelicEffectType effectType,
            Action<int> action)
        {
            for (int i = 0; i < ownedRelics.Count; i++)
            {
                RelicData relic = ownedRelics[i];
                if (relic == null ||
                    relic.effectType != effectType ||
                    relic.triggerEveryCards <= 0 ||
                    cardsPlayedThisCombat % relic.triggerEveryCards != 0)
                {
                    continue;
                }

                action?.Invoke(Mathf.Max(0, relic.amount));
            }
        }

        void ResetCombatState()
        {
            cardsPlayedThisCombat = 0;
            firstHitReductionAvailable =
                GetTotalAmount(RelicEffectType.ReduceFirstHit) > 0;
        }

        void RefreshUI()
        {
            if (inventoryUI != null)
                inventoryUI.Refresh(ownedRelics);
        }
    }
}
