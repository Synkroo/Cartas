using UnityEngine;

namespace JuegoDeCartas.Relics
{
    public enum RelicEffectType
    {
        BuffDuration,
        LifeSteal,
        CombatStartArmor,
        TurnStartArmor,
        HealAfterCombat,
        FlatInterest,
        FirstCardDamage,
        DrawEveryCards,
        ReduceFirstHit,
        ZeroCostCardArmor,
        RestoreManaEveryCards,
        GoldPerEnemy
    }

    public enum RelicRarity
    {
        Comun,
        Rara,
        Epica
    }

    [CreateAssetMenu(fileName = "NuevaReliquia", menuName = "Tienda/Reliquia")]
    public class RelicData : ScriptableObject
    {
        [Header("Info")]
        public string relicName;
        [TextArea(2, 5)] public string description;
        public Sprite icon;
        public RelicRarity rarity;

        [Header("Shop")]
        [Min(0)] public int price = 300;
        [Min(0f)] public float appearanceWeight = 1f;

        [Header("Effect")]
        public RelicEffectType effectType;
        [Min(0)] public int amount;
        [Min(0)] public int triggerEveryCards;
        [Range(0, 100)] public int percentage;

        void OnValidate()
        {
            price = Mathf.Max(0, price);
            appearanceWeight = Mathf.Max(0f, appearanceWeight);
            amount = Mathf.Max(0, amount);
            triggerEveryCards = Mathf.Max(0, triggerEveryCards);
            percentage = Mathf.Clamp(percentage, 0, 100);
        }
    }
}
