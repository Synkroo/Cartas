using System.Collections.Generic;
using UnityEngine;
using JuegoDeCartas.Enemies;

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
            PlayerPrefs.SetInt(GetCompletionKey(difficulty), 1);
            PlayerPrefs.Save();
        }

        public float GetEnemyStatMultiplier(MissionDifficulty difficulty)
        {
            return difficulty >= MissionDifficulty.Media ? 1.2f : 1f;
        }

        public float GetShopCostMultiplier(MissionDifficulty difficulty)
        {
            return difficulty >= MissionDifficulty.Letal ? 1.2f : 1f;
        }

        string GetCompletionKey(MissionDifficulty difficulty)
        {
            return "MissionCompleted_" + name + "_" + difficulty;
        }
    }
}
