using UnityEngine;
using JuegoDeCartas.Managers;

namespace JuegoDeCartas.Cards
{
    public abstract class CardEffect : ScriptableObject
    {
        public abstract void Apply(BattleManager battle);
    }
}
