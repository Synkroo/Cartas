using System.Collections.Generic;
using UnityEngine;
using JuegoDeCartas.Cards;

namespace JuegoDeCartas.Characters
{
    [CreateAssetMenu(fileName = "NuevoPersonaje", menuName = "Juego de Cartas/Personajes/Personaje")]
    public class CharacterData : ScriptableObject
    {
        [Header("Info")]
        public string characterName;
        public Sprite portrait;
        [TextArea(3, 6)] public string description;
        [TextArea(2, 5)] public string mechanicDescription;

        [Header("Starting Stats")]
        [Min(1)] public int maxHealth = 50;
        [Min(0)] public int maxMana = 3;
        [Min(0)] public int cardsPerTurn = 4;
        [Min(0)] public int startingGold = 300;

        [Header("Deck")]
        public List<CardData> startingDeck = new List<CardData>();

        [Header("Unlock")]
        public bool unlockedByDefault;
        public bool comingSoon;
        public CharacterUnlockCondition unlockCondition;

        public bool IsUnlocked => unlockedByDefault || (unlockCondition != null && unlockCondition.IsMet());
        public bool IsSelectable => IsUnlocked && !comingSoon;

        public string GetLockedMessage()
        {
            if (comingSoon)
            {
                if (unlockCondition != null &&
                    !unlockCondition.IsMet() &&
                    !string.IsNullOrWhiteSpace(unlockCondition.lockedMessage))
                {
                    return "Proximamente - " + unlockCondition.lockedMessage;
                }

                return "Proximamente";
            }

            if (unlockCondition != null && !string.IsNullOrWhiteSpace(unlockCondition.lockedMessage))
                return unlockCondition.lockedMessage;

            return "Personaje bloqueado";
        }

        void OnValidate()
        {
            maxHealth = Mathf.Max(1, maxHealth);
            maxMana = Mathf.Max(0, maxMana);
            cardsPerTurn = Mathf.Max(0, cardsPerTurn);
            startingGold = Mathf.Max(0, startingGold);
        }
    }
}
