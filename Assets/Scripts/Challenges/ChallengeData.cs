using JuegoDeCartas.Characters;
using JuegoDeCartas.Progression;
using UnityEngine;

namespace JuegoDeCartas.Challenges
{
    public enum ChallengeModifier
    {
        SeededRun,
        BossRush,
        NoShop,
        SingleClass
    }

    [CreateAssetMenu(
        fileName = "NuevoReto",
        menuName = "Juego de Cartas/Retos/Reto"
    )]
    public class ChallengeData : ScriptableObject
    {
        [Header("Info")]
        public string challengeName;
        [TextArea(3, 6)] public string description;
        public Sprite image;

        [Header("Rules")]
        public ChallengeModifier modifier;
        public CharacterData requiredCharacter;
        [Min(0)] public int combatCountOverride;

        public bool IsCompleted =>
            ProfileManager.IsTemporary ||
            ProfilePrefs.GetInt(GetCompletionKey(), 0) == 1;

        public bool AllowsCharacter(CharacterData character)
        {
            return modifier != ChallengeModifier.SingleClass ||
                   requiredCharacter == null ||
                   character == requiredCharacter;
        }

        public string GetRuleSummary()
        {
            return modifier switch
            {
                ChallengeModifier.BossRush =>
                    "Todos los combates son contra el jefe de la mision.",
                ChallengeModifier.NoShop =>
                    "No aparecen tiendas entre combates.",
                ChallengeModifier.SingleClass when requiredCharacter != null =>
                    "Solo puede jugarse con " + requiredCharacter.characterName + ".",
                ChallengeModifier.SingleClass =>
                    "Solo puede jugarse con la clase configurada.",
                _ => "Run normal reproducible mediante su codigo de semilla."
            };
        }

        public void MarkCompleted()
        {
            ProfilePrefs.SetInt(GetCompletionKey(), 1);
            ProfilePrefs.Save();
        }

        string GetCompletionKey()
        {
            return "ChallengeCompleted_" + name;
        }

        void OnValidate()
        {
            combatCountOverride = Mathf.Max(0, combatCountOverride);
        }
    }
}
