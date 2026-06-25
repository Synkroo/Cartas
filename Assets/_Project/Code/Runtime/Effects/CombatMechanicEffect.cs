using UnityEngine;
using JuegoDeCartas.Cards;
using JuegoDeCartas.Enemies;
using JuegoDeCartas.Managers;

namespace JuegoDeCartas.Effects
{
    public enum CombatMechanicAction
    {
        DealDamage,
        GainArmor,
        RestoreMana,
        DrawCards,
        FreezeCards,
        ShatterFrozenCards,
        BurnCards,
        DamagePerFrozenCard,
        DamageByMissingHandCards,
        DamageByBurnedCardsThisTurn,
        DamagePerEnemyStatusStack,
        ApplyEnemyStatus,
        CriticalDamageFromWeakness,
        DetonateBleed,
        SpendManaForDamage,
        SpendHealthForDamage
    }

    [CreateAssetMenu(menuName = "Cards/Effects/Combat Mechanic")]
    public class CombatMechanicEffect : CardEffect
    {
        public CombatMechanicAction action;
        public EnemyStatusType statusType;
        [Min(0)] public int amount;
        [Min(0)] public int secondaryAmount;
        [Min(0)] public int threshold;
        [Min(0)] public int costReduction;
        [Min(0)] public int reactivations;
        [Range(0, 300)] public int percentage = 100;
        public bool consumeStatus = true;
        public bool repeatWithReactivationMultiplier;

        public override bool UsesReactivationMultiplier =>
            repeatWithReactivationMultiplier;
        public bool RequiresCardTarget =>
            action == CombatMechanicAction.FreezeCards ||
            action == CombatMechanicAction.BurnCards ||
            action == CombatMechanicAction.ShatterFrozenCards;

        public override void Apply(BattleManager battle)
        {
            Apply(battle, 1);
        }

        public override void Apply(
            BattleManager battle,
            int reactivationMultiplier)
        {
            if (battle == null)
                return;

            int repeats = repeatWithReactivationMultiplier
                ? Mathf.Max(1, reactivationMultiplier)
                : 1;
            for (int i = 0; i < repeats; i++)
                ApplyOnce(battle);
        }

        void ApplyOnce(BattleManager battle)
        {
            switch (action)
            {
                case CombatMechanicAction.DealDamage:
                    battle.DamageEnemy(amount);
                    break;
                case CombatMechanicAction.GainArmor:
                    battle.GainPlayerArmor(amount);
                    break;
                case CombatMechanicAction.RestoreMana:
                    battle.RestorePlayerMana(amount);
                    break;
                case CombatMechanicAction.DrawCards:
                    battle.DrawCards(amount);
                    break;
                case CombatMechanicAction.FreezeCards:
                    ApplyFreezeCards(battle);
                    break;
                case CombatMechanicAction.ShatterFrozenCards:
                    ApplyShatterFrozenCards(battle);
                    break;
                case CombatMechanicAction.BurnCards:
                    ApplyBurnCards(battle);
                    break;
                case CombatMechanicAction.DamagePerFrozenCard:
                    battle.DamageEnemy(
                        amount + battle.FrozenCardCount * secondaryAmount
                    );
                    break;
                case CombatMechanicAction.DamageByMissingHandCards:
                    ApplyMissingHandDamage(battle);
                    break;
                case CombatMechanicAction.DamageByBurnedCardsThisTurn:
                    battle.DamageEnemy(
                        amount + battle.BurnedCardsThisTurn * secondaryAmount
                    );
                    break;
                case CombatMechanicAction.DamagePerEnemyStatusStack:
                    ApplyStatusScalingDamage(battle);
                    break;
                case CombatMechanicAction.ApplyEnemyStatus:
                    battle.ApplyEnemyStatus(statusType, amount);
                    break;
                case CombatMechanicAction.CriticalDamageFromWeakness:
                    battle.DamageEnemyWithWeakness(
                        amount,
                        threshold,
                        percentage,
                        consumeStatus
                    );
                    break;
                case CombatMechanicAction.DetonateBleed:
                    battle.DetonateBleed(percentage);
                    break;
                case CombatMechanicAction.SpendManaForDamage:
                    ApplySpendManaDamage(battle);
                    break;
                case CombatMechanicAction.SpendHealthForDamage:
                    ApplySpendHealthDamage(battle);
                    break;
            }
        }

        void ApplyBurnCards(BattleManager battle)
        {
            Card target = null;
            battle.TryConsumePreparedMechanicTarget(this, out target);
            int burned = battle.BurnCardsFromHand(amount, target);
            int damage = burned * secondaryAmount;
            if (damage > 0)
                battle.DamageEnemy(damage);
        }

        void ApplyFreezeCards(BattleManager battle)
        {
            Card target = null;
            battle.TryConsumePreparedMechanicTarget(this, out target);
            battle.FreezeCardsFromHand(amount, target);
        }

        void ApplyShatterFrozenCards(BattleManager battle)
        {
            Card target = null;
            battle.TryConsumePreparedMechanicTarget(this, out target);
            battle.ShatterFrozenCards(
                amount,
                secondaryAmount,
                costReduction,
                reactivations,
                target
            );
        }

        void ApplyMissingHandDamage(BattleManager battle)
        {
            if (battle.deckManager == null)
                return;

            int referenceSize = threshold > 0
                ? threshold
                : battle.deckManager.cardsPerTurn;
            int missing = Mathf.Max(
                0,
                referenceSize - battle.deckManager.hand.Count
            );
            battle.DamageEnemy(amount + missing * secondaryAmount);
        }

        void ApplyStatusScalingDamage(BattleManager battle)
        {
            int stacks = battle.enemy != null
                ? battle.enemy.GetStatus(statusType)
                : 0;
            battle.DamageEnemy(amount + stacks * secondaryAmount);
        }

        void ApplySpendManaDamage(BattleManager battle)
        {
            if (battle.player == null)
                return;

            int spent = Mathf.Min(
                Mathf.Max(0, amount),
                Mathf.Max(0, battle.player.stats.mana)
            );
            if (spent <= 0)
                return;

            battle.player.stats.mana -= spent;
            battle.DamageEnemy(spent * Mathf.Max(0, secondaryAmount));
        }

        void ApplySpendHealthDamage(BattleManager battle)
        {
            if (battle.player == null)
                return;

            int before = battle.player.stats.health;
            battle.DamagePlayer(amount);
            if (battle.IsBattleEnded)
                return;

            int spent = Mathf.Max(0, before - battle.player.stats.health);
            if (spent > 0)
                battle.DamageEnemy(spent * Mathf.Max(0, secondaryAmount));
        }
    }
}
