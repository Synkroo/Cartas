using JuegoDeCartas.Characters;
using JuegoDeCartas.Missions;
using JuegoDeCartas.Progression;
using UnityEngine;

namespace JuegoDeCartas.Challenges
{
    public enum ChallengeDifficulty
    {
        Facil,
        Media,
        Dificil
    }

    public enum ChallengeModifier
    {
        Concept = 0,
        BossRush = 1,
        NoShop = 2,
        SingleClass = 3
    }

    [CreateAssetMenu(
        fileName = "NuevoReto",
        menuName = "Juego de Cartas/Retos/Reto"
    )]
    public class ChallengeData : StableContentData
    {
        [Header("Info")]
        public string challengeName;
        [TextArea(3, 6)] public string description;
        public Sprite image;
        public ChallengeDifficulty difficulty = ChallengeDifficulty.Media;
        public bool implemented = true;
        [TextArea(2, 5)] public string ruleSummary;

        [Header("Special Mission")]
        [Tooltip("Configuracion de enemigos usada por este reto. No aparece entre las misiones normales.")]
        public MissionData encounterMission;

        [Header("Rules")]
        public ChallengeModifier modifier;
        public CharacterData requiredCharacter;
        [Min(0)] public int combatCountOverride;

        public bool IsCompleted =>
            implemented &&
            (ProfileManager.IsTemporary ||
             ProfilePrefs.GetIntMigrating(
                 GetCompletionKey(),
                 GetLegacyCompletionKey(),
                 0
             ) == 1);

        public bool CountsForCompletion => implemented;
        public bool IsPlayable => implemented && encounterMission != null;

        public bool IsCompletedByCharacter(CharacterData character)
        {
            return implemented &&
                   character != null &&
                   (ProfileManager.IsTemporary ||
                    ProfilePrefs.GetIntMigrating(
                        GetCharacterCompletionKey(character),
                        GetLegacyCharacterCompletionKey(character),
                        0
                    ) == 1);
        }

        public bool AllowsCharacter(CharacterData character)
        {
            return modifier != ChallengeModifier.SingleClass ||
                   requiredCharacter == null ||
                   character == requiredCharacter;
        }

        public string GetRuleSummary()
        {
            if (!string.IsNullOrWhiteSpace(ruleSummary))
                return ruleSummary;

            return modifier switch
            {
                ChallengeModifier.BossRush =>
                    "Todos los combates son contra el jefe configurado para este reto.",
                ChallengeModifier.NoShop =>
                    "No aparecen tiendas entre combates.",
                ChallengeModifier.SingleClass when requiredCharacter != null =>
                    "Solo puede jugarse con " + requiredCharacter.characterName + ".",
                ChallengeModifier.SingleClass =>
                    "Solo puede jugarse con la clase configurada.",
                _ => "Mision especial con reglas propias."
            };
        }

        public string GetDifficultyLabel()
        {
            return difficulty switch
            {
                ChallengeDifficulty.Facil => "FACIL",
                ChallengeDifficulty.Dificil => "DIFICIL",
                _ => "MEDIA"
            };
        }

        public void MarkCompleted(CharacterData character)
        {
            if (!implemented)
                return;

            ProfilePrefs.SetInt(GetCompletionKey(), 1);
            if (character != null)
                ProfilePrefs.SetInt(GetCharacterCompletionKey(character), 1);
            ProfilePrefs.Save();
        }

        string GetCompletionKey()
        {
            return "ChallengeCompleted_" + ContentId;
        }

        string GetCharacterCompletionKey(CharacterData character)
        {
            return GetCompletionKey() + "_" +
                   ContentIdUtility.GetId(character);
        }

        string GetLegacyCompletionKey()
        {
            return "ChallengeCompleted_" + name;
        }

        string GetLegacyCharacterCompletionKey(CharacterData character)
        {
            return GetLegacyCompletionKey() + "_" + character.name;
        }

        void OnValidate()
        {
            EnsureContentId();
            combatCountOverride = Mathf.Max(0, combatCountOverride);
        }
    }
}
