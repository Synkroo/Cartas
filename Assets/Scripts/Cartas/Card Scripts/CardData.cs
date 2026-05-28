using System.Collections.Generic;
using UnityEngine;
using JuegoDeCartas.Effects;

namespace JuegoDeCartas.Cards
{
    [CreateAssetMenu(fileName = "New Card", menuName = "Cards/Card")]
    public class CardData : ScriptableObject
    {
        public string cardName;
        [TextArea(2, 5)] public string description;
        public Sprite sprite;
        public int cost;
        public bool destroyOnUse;
        public List<CardEffect> effects = new List<CardEffect>();
    }
}
