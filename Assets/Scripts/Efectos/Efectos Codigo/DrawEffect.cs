using UnityEngine;
using JuegoDeCartas.Managers;

namespace JuegoDeCartas.Effects
{
    [CreateAssetMenu(menuName = "Cards/Effects/Draw Cards")]
    public class DrawEffect : CardEffect
    {
        public int amount;

        public override void Apply(BattleManager battle)
        {
            battle.DrawCards(amount);
        }
    }
}
