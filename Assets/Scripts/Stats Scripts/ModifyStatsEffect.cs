using System.Collections.Generic;
using UnityEngine;

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

    void ApplyPlayerStat(Stats stats, StatModifier mod)
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

        stats.Clamp();
    }
    void ApplyEnemyStat(BattleManager battle, StatModifier mod)
    {
        if (battle.enemy == null)
            return;

        int value = mod.operation == StatModifier.Operation.Add
            ? mod.amount
            : -mod.amount;

        switch (mod.stat)
        {
            case StatType.Health:

                battle.enemy.currentHealth += value;

                battle.enemy.currentHealth = Mathf.Clamp(
                    battle.enemy.currentHealth,
                    0,
                    battle.enemy.data.maxHealth
                );

                if (battle.enemy.currentHealth <= 0)
                {
                    battle.DamageEnemy(999999);
                }

                break;
        }
    }
}