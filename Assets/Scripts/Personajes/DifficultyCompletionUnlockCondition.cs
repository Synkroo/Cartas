using System.Collections.Generic;
using UnityEngine;
using JuegoDeCartas.Missions;

namespace JuegoDeCartas.Characters
{
    [CreateAssetMenu(fileName = "DifficultyUnlock", menuName = "Juego de Cartas/Personajes/Desbloqueo por dificultad")]
    public class DifficultyCompletionUnlockCondition : CharacterUnlockCondition
    {
        public MissionDifficulty requiredDifficulty = MissionDifficulty.Media;
        public List<MissionData> missions = new List<MissionData>();

        public override bool IsMet()
        {
            if (MissionData.IsAnyDifficultyCompleted(requiredDifficulty))
                return true;

            for (int i = 0; i < missions.Count; i++)
            {
                if (missions[i] != null && missions[i].IsDifficultyCompleted(requiredDifficulty))
                    return true;
            }

            return false;
        }
    }
}
