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
        [TextArea(1, 2)] public string cardDescription;
        [Min(0)] public int costReduction;
        [Min(0)] public int reactivations;
        public bool preventDestroyOnUse;
        public List<CardEffect> bonusEffects = new List<CardEffect>();

        public string GetCardDescription()
        {
            return string.IsNullOrWhiteSpace(cardDescription)
                ? description
                : cardDescription;
        }
    }

    [System.Serializable]
    public class CardEpiphany
    {
        public string epiphanyName;
        [TextArea(2, 4)] public string description;
        [TextArea(1, 2)] public string cardDescription;
        [Min(0)] public int costReduction;
        [Min(0)] public int reactivations;
        public bool preventDestroyOnUse;
        public List<CardEffect> bonusEffects = new List<CardEffect>();

        public string GetCardDescription()
        {
            return string.IsNullOrWhiteSpace(cardDescription)
                ? description
                : cardDescription;
        }
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

        [Header("Epiphanies")]
        public List<CardEpiphany> epiphanyOptions = new List<CardEpiphany>();
        [HideInInspector] public CardEpiphany epiphany = new CardEpiphany();

        public List<CardEpiphany> GetEpiphanyOptions()
        {
            if (epiphanyOptions != null && epiphanyOptions.Count > 0)
                return epiphanyOptions;

            if (epiphany != null &&
                !string.IsNullOrWhiteSpace(epiphany.epiphanyName))
            {
                return new List<CardEpiphany> { epiphany };
            }

            return new List<CardEpiphany>();
        }

        void OnValidate()
        {
            cost = Mathf.Max(0, cost);
        }
    }
}
