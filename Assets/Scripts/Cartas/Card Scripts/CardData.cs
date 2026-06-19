using System.Collections.Generic;
using UnityEngine;
using JuegoDeCartas.Effects;

namespace JuegoDeCartas.Cards
{
    [System.Serializable]
    public class CardUpgradeOption
    {
        public string upgradeName;
        [TextArea(2, 4)] public string description;
        [Min(0)] public int costReduction;
        [Min(0)] public int reactivations;
        public bool preventDestroyOnUse;
        public List<CardEffect> bonusEffects = new List<CardEffect>();
    }

    [CreateAssetMenu(fileName = "New Card", menuName = "Cards/Card")]
    public class CardData : ScriptableObject
    {
        public string cardName;
        [TextArea(2, 5)] public string description;
        public Sprite sprite;
        public int cost;
        public bool destroyOnUse;
        public List<CardEffect> effects = new List<CardEffect>();

        [Header("Upgrades")]
        public List<CardUpgradeOption> upgradeOptions = new List<CardUpgradeOption>();

        void OnValidate()
        {
            cost = Mathf.Max(0, cost);
        }
    }
}
