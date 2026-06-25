namespace JuegoDeCartas.Missions
{
    public static class MissionRunState
    {
        public static MissionData SelectedMission { get; private set; }
        public static MissionDifficulty SelectedDifficulty { get; private set; } = MissionDifficulty.Facil;

        public static bool HasMission => SelectedMission != null;
        public static float EnemyStatMultiplier =>
            SelectedMission != null ? SelectedMission.GetEnemyStatMultiplier(SelectedDifficulty) : 1f;
        public static float ShopCostMultiplier =>
            SelectedMission != null ? SelectedMission.GetShopCostMultiplier(SelectedDifficulty) : 1f;

        public static void SelectMission(MissionData mission, MissionDifficulty difficulty)
        {
            SelectedMission = mission;
            SelectedDifficulty = difficulty;
        }

        public static void Clear()
        {
            SelectedMission = null;
            SelectedDifficulty = MissionDifficulty.Facil;
        }
    }
}
