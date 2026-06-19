using System.Collections.Generic;
using UnityEngine;
using JuegoDeCartas.Enemies;
using JuegoDeCartas.Characters;

namespace JuegoDeCartas.Missions
{
    [CreateAssetMenu(fileName = "NuevaMision", menuName = "Juego de Cartas/Misiones/Mision")]
    public class MissionData : ScriptableObject
    {
        [Header("Info")]
        public string missionName;
        public Sprite image;
        [TextArea(3, 6)] public string description;

        [Header("Enemies")]
        public EnemyData boss;
        public List<EnemyData> possibleNormalEnemies = new List<EnemyData>();
        public List<EnemyData> possibleMiniBosses = new List<EnemyData>();

        [Header("Run")]
        [Min(1)] public int combatCount = 5;
        [Min(1)] public int miniBossFrequency = 3;

        public Sprite DisplaySprite => image != null ? image : boss != null ? boss.sprite : null;

        public bool IsDifficultyUnlocked(MissionDifficulty difficulty)
        {
            if (difficulty == MissionDifficulty.Facil)
                return true;

            return IsDifficultyCompleted((MissionDifficulty)((int)difficulty - 1));
        }

        public bool IsDifficultyCompleted(MissionDifficulty difficulty)
        {
            return PlayerPrefs.GetInt(GetCompletionKey(difficulty), 0) == 1;
        }

        public void MarkCompleted(MissionDifficulty difficulty)
        {
            MarkCompleted(difficulty, CharacterRunState.SelectedCharacter);
        }

        public void MarkCompleted(MissionDifficulty difficulty, CharacterData character)
        {
            PlayerPrefs.SetInt(GetCompletionKey(difficulty), 1);
            PlayerPrefs.SetInt(GetGlobalCompletionKey(difficulty), 1);
            if (character != null)
            {
                PlayerPrefs.SetInt(GetCharacterCompletionKey(difficulty, character), 1);
                PlayerPrefs.SetInt(GetGlobalCharacterCompletionKey(difficulty, character), 1);
            }
            PlayerPrefs.Save();
        }

        public bool IsDifficultyCompletedByCharacter(MissionDifficulty difficulty, CharacterData character)
        {
            return character != null &&
                   PlayerPrefs.GetInt(GetCharacterCompletionKey(difficulty, character), 0) == 1;
        }

        public static bool IsAnyDifficultyCompleted(MissionDifficulty difficulty)
        {
            return PlayerPrefs.GetInt(GetGlobalCompletionKey(difficulty), 0) == 1;
        }

        public static bool IsAnyDifficultyCompletedByCharacter(
            MissionDifficulty difficulty,
            CharacterData character)
        {
            return character != null &&
                   PlayerPrefs.GetInt(GetGlobalCharacterCompletionKey(difficulty, character), 0) == 1;
        }

        public float GetEnemyStatMultiplier(MissionDifficulty difficulty)
        {
            return difficulty >= MissionDifficulty.Letal ? 1.2f : 1f;
        }

        public float GetShopCostMultiplier(MissionDifficulty difficulty)
        {
            return difficulty >= MissionDifficulty.Media ? 1.2f : 1f;
        }

        void OnValidate()
        {
            combatCount = Mathf.Max(1, combatCount);
            miniBossFrequency = Mathf.Max(1, miniBossFrequency);
        }

        string GetCompletionKey(MissionDifficulty difficulty)
        {
            return "MissionCompleted_" + name + "_" + difficulty;
        }

        static string GetGlobalCompletionKey(MissionDifficulty difficulty)
        {
            return "AnyMissionCompleted_" + difficulty;
        }

        string GetCharacterCompletionKey(MissionDifficulty difficulty, CharacterData character)
        {
            return "MissionCompleted_" + name + "_" + difficulty + "_" + character.name;
        }

        static string GetGlobalCharacterCompletionKey(
            MissionDifficulty difficulty,
            CharacterData character)
        {
            return "AnyMissionCompleted_" + difficulty + "_" + character.name;
        }
    }
}
