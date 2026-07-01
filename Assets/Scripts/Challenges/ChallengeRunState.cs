using JuegoDeCartas.Characters;
using JuegoDeCartas.Missions;

namespace JuegoDeCartas.Challenges
{
    public static class ChallengeRunState
    {
        public static ChallengeData SelectedChallenge { get; private set; }
        public static string SeedCode { get; private set; } = "";
        public static bool IsSeededRun { get; private set; }

        public static bool IsActive => SelectedChallenge != null;
        public static bool IsChallengeRun => SelectedChallenge != null;
        public static bool HasConfiguredRun => IsChallengeRun || IsSeededRun;
        public static bool IsBossRush =>
            SelectedChallenge != null &&
            SelectedChallenge.modifier == ChallengeModifier.BossRush;
        public static bool IsShopDisabled =>
            SelectedChallenge != null &&
            SelectedChallenge.modifier == ChallengeModifier.NoShop;
        public static int CombatCountOverride =>
            SelectedChallenge != null ? SelectedChallenge.combatCountOverride : 0;
        public static CharacterData RequiredCharacter =>
            SelectedChallenge != null &&
            SelectedChallenge.modifier == ChallengeModifier.SingleClass
                ? SelectedChallenge.requiredCharacter
                : null;
        public static MissionData EncounterMission =>
            SelectedChallenge != null ? SelectedChallenge.encounterMission : null;

        public static void ConfigureChallenge(ChallengeData challenge)
        {
            SelectedChallenge = challenge;
            SeedCode = "";
            IsSeededRun = false;
        }

        public static void ConfigureSeededRun(string seedCode)
        {
            SelectedChallenge = null;
            SeedCode = RunRandom.NormalizeSeedCode(seedCode);
            IsSeededRun = true;
        }

        public static void PrepareRun()
        {
            if (IsSeededRun)
                RunRandom.Initialize(SeedCode);
            else
                RunRandom.InitializeUnseeded();
        }

        public static bool AllowsCharacter(CharacterData character)
        {
            return !IsActive || SelectedChallenge.AllowsCharacter(character);
        }

        public static void MarkCompleted(CharacterData character)
        {
            if (SelectedChallenge != null)
                SelectedChallenge.MarkCompleted(character);
        }

        public static void Clear()
        {
            SelectedChallenge = null;
            SeedCode = "";
            IsSeededRun = false;
        }
    }
}
