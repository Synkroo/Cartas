using UnityEngine;

namespace JuegoDeCartas.Characters
{
    public enum SubclassPassiveType
    {
        ArmorToDamage,
        StackDamageBuffs,
        RetainArmor,
        BonusMaxMana,
        DrawEveryCards,
        RestoreManaEveryCards,
        FirstCardBonusDamage,
        GoldEveryCards,
        ArmorEveryCards
    }

    [CreateAssetMenu(fileName = "NuevaSubclase", menuName = "Juego de Cartas/Personajes/Subclase")]
    public class SubclassData : ScriptableObject
    {
        [Header("Info")]
        public string subclassName;
        public Sprite icon;
        [TextArea(2, 5)] public string description;
        [TextArea(2, 5)] public string passiveDescription;

        [Header("Owner")]
        public CharacterData character;

        [Header("Passive")]
        public SubclassPassiveType passiveType;
        [Min(0)] public int amount;
        [Min(1)] public int triggerCount = 1;
        [Range(0, 100)] public int percentage;

        void OnValidate()
        {
            amount = Mathf.Max(0, amount);
            triggerCount = Mathf.Max(1, triggerCount);
            percentage = Mathf.Clamp(percentage, 0, 100);
        }
    }
}
