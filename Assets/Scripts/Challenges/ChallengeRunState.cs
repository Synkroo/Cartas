using JuegoDeCartas.Characters;

namespace JuegoDeCartas.Challenges
{
    public static class ChallengeRunState
    {
        public static ChallengeData SelectedChallenge { get; private set; }
        public static string SeedCode { get; private set; } = "";

        public static bool IsActive => SelectedChallenge != null;
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

        public static void Configure(ChallengeData challenge, string seedCode)
        {
            SelectedChallenge = challenge;
            SeedCode = RunRandom.NormalizeSeedCode(seedCode);
        }

        public static void PrepareRun()
        {
            if (IsActive)
                RunRandom.Initialize(SeedCode);
            else
                RunRandom.InitializeUnseeded();
        }

        public static bool AllowsCharacter(CharacterData character)
        {
            return !IsActive || SelectedChallenge.AllowsCharacter(character);
        }

        public static void MarkCompleted()
        {
            if (SelectedChallenge != null)
                SelectedChallenge.MarkCompleted();
        }

        public static void Clear()
        {
            SelectedChallenge = null;
            SeedCode = "";
        }
    }
}
