namespace JuegoDeCartas.Characters
{
    public static class CharacterRunState
    {
        public static CharacterData SelectedCharacter { get; private set; }

        public static bool HasCharacter => SelectedCharacter != null;

        public static void Select(CharacterData character)
        {
            SelectedCharacter = character;
        }

        public static void Clear()
        {
            SelectedCharacter = null;
        }
    }
}
