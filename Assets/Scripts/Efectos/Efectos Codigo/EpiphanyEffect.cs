using UnityEngine;
using JuegoDeCartas.Managers;

namespace JuegoDeCartas.Effects
{
    [CreateAssetMenu(menuName = "Cards/Effects/Epiphany")]
    public class EpiphanyEffect : CardEffect
    {
        [Min(0)] public int damage;
        [Min(0)] public int armor;
        [Min(0)] public int cardsToDraw;
        [Min(0)] public int manaToRestore;
        [Min(0)] public int healthToRestore;
        [Min(0)] public int goldToGain;
        [Min(0)] public int damageBonus;
        [Min(0)] public int damageBonusTurns;

        public override void Apply(BattleManager battle)
        {
            if (battle == null)
                return;

            if (damage > 0)
                battle.DamageEnemy(damage);
            if (armor > 0)
                battle.GainPlayerArmor(armor);
            if (cardsToDraw > 0)
                battle.DrawCards(cardsToDraw);
            if (manaToRestore > 0)
                battle.RestorePlayerMana(manaToRestore);
            if (healthToRestore > 0 && battle.player != null)
            {
                battle.player.stats.health = Mathf.Min(
                    battle.player.stats.maxHealth,
                    battle.player.stats.health + healthToRestore
                );
                battle.player.stats.Clamp();
            }
            if (goldToGain > 0 && battle.gameManager != null)
                battle.gameManager.dinero += goldToGain;
            if (damageBonus > 0 && damageBonusTurns > 0)
                battle.ApplyPlayerDamageBonus(damageBonus, damageBonusTurns);

            battle.UpdateUI();
        }
    }
}
