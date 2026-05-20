using UnityEngine;
using JuegoDeCartas.Managers;

namespace JuegoDeCartas.Effects
{
    public abstract class CardEffect : ScriptableObject
    {
        public abstract void Apply(BattleManager battle);
    }
}
