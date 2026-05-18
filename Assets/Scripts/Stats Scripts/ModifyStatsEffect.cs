using System.Collections.Generic;
using UnityEngine;
using JuegoDeCartas.Cards;
using JuegoDeCartas.Managers;
using JuegoDeCartas.Stats;

namespace JuegoDeCartas.Effects
{
    [CreateAssetMenu(menuName = "Cards/Effects/Modify Stats")]
    public class ModifyStatsEffect : CardEffect
    {
        public List<StatModifier> modifiers = new List<StatModifier>();

        public override void Apply(BattleManager battle)
        {
            foreach (var mod in modifiers)
            {
                ApplyToTarget(battle, mod);
            }

            battle.player.stats.Clamp();
            battle.UpdateUI();
        }

        void ApplyToTarget(BattleManager battle, StatModifier mod)
        {
            if (mod.target == StatModifier.Target.Player ||
                mod.target == StatModifier.Target.Both)
            {
                ApplyPlayerStat(battle.player.stats, mod);
            }

            if (mod.target == StatModifier.Target.Enemy ||
                mod.target == StatModifier.Target.Both)
            {
                ApplyEnemyStat(battle, mod);
            }
        }

        void ApplyPlayerStat(JuegoDeCartas.Stats.Stats stats, StatModifier mod)
        {
            int value = mod.operation == StatModifier.Operation.Add
                ? mod.amount
                : -mod.amount;

            switch (mod.stat)
            {
                case StatType.Health:
                    stats.health += value;
                    break;

                case StatType.MaxHealth:
                    stats.maxHealth += value;
                    break;

                case StatType.Mana:
                    stats.mana += value;
                    break;

                case StatType.MaxMana:
                    stats.maxMana += value;
                    break;

                case StatType.Armor:
                    stats.armor += value;
                    break;
            }
        }

        void ApplyEnemyStat(BattleManager battle, StatModifier mod)
        {
            if (battle.enemy == null)
                return;

            switch (mod.stat)
            {
                case StatType.Health:
                    if (mod.operation == StatModifier.Operation.Remove)
                    {
                        battle.DamageEnemy(mod.amount);
                    }
                    else
                    {
                        battle.enemy.currentHealth += mod.amount;
                        battle.enemy.currentHealth = Mathf.Clamp(
                            battle.enemy.currentHealth, 0, battle.enemy.maxHealth);
                    }
                    break;

                case StatType.MaxHealth:
                    if (mod.operation == StatModifier.Operation.Add)
                    {
                        battle.enemy.maxHealth += mod.amount;
                    }
                    else
                    {
                        battle.enemy.maxHealth -= mod.amount;
                        battle.enemy.maxHealth = Mathf.Max(1, battle.enemy.maxHealth);
                    }
                    battle.enemy.currentHealth = Mathf.Clamp(
                        battle.enemy.currentHealth, 0, battle.enemy.maxHealth);
                    break;

                case StatType.Damage:
                    if (mod.operation == StatModifier.Operation.Add)
                    {
                        battle.enemy.damageModifier += mod.amount;
                    }
                    else
                    {
                        battle.enemy.damageModifier -= mod.amount;
                    }
                    break;

                case StatType.Armor:
                case StatType.Mana:
                case StatType.MaxMana:
                    break;
            }
        }
    }
}
