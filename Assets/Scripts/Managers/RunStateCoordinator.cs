using JuegoDeCartas.Challenges;
using JuegoDeCartas.Characters;
using JuegoDeCartas.Missions;

namespace JuegoDeCartas.Managers
{
    public static class RunStateCoordinator
    {
        public static void Reset()
        {
            CharacterRunState.Clear();
            MissionRunState.Clear();
            ChallengeRunState.Clear();
            RunRandom.Reset();
        }
    }
}
