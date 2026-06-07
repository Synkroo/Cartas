using UnityEngine;
using JuegoDeCartas.Managers;

namespace JuegoDeCartas.Effects
{
    public abstract class CardEffect : ScriptableObject
    {
        public virtual bool UsesReactivationMultiplier => false;

        public abstract void Apply(BattleManager battle);

        public virtual void Apply(BattleManager battle, int reactivationMultiplier)
        {
            Apply(battle);
        }
    }
}
