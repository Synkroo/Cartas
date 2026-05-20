using System.Collections.Generic;
using UnityEngine;
using JuegoDeCartas.Effects;

namespace JuegoDeCartas.Cards
{
    [CreateAssetMenu(fileName = "New Card", menuName = "Cards/Card")]
    public class CardData : ScriptableObject
    {
        public string cardName;
        public int cost;
        public List<CardEffect> effects;
    }
}
