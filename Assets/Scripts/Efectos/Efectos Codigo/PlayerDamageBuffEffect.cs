using UnityEngine;
using JuegoDeCartas.Managers;

namespace JuegoDeCartas.Effects
{
    [CreateAssetMenu(menuName = "Cards/Effects/Player Damage Buff")]
    public class PlayerDamageBuffEffect : CardEffect
    {
        public int amount = 5;
        public int turns = 3;

        public override void Apply(BattleManager battle)
        {
            if (battle == null)
                return;

            battle.ApplyPlayerDamageBonus(amount, turns);
            battle.UpdateUI();
        }
    }
}
