using UnityEngine;
using JuegoDeCartas.Managers;

namespace JuegoDeCartas.Effects
{
    [CreateAssetMenu(menuName = "Cards/Effects/Player Damage Buff")]
    public class PlayerDamageBuffEffect : CardEffect
    {
        public int amount = 5;
        public int turns = 3;

        public override bool UsesReactivationMultiplier => true;

        public override void Apply(BattleManager battle)
        {
            Apply(battle, 1);
        }

        public override void Apply(BattleManager battle, int reactivationMultiplier)
        {
            if (battle == null)
                return;

            battle.ApplyPlayerDamageBonus(amount * Mathf.Max(1, reactivationMultiplier), turns);
            battle.UpdateUI();
        }
    }
}
