namespace JuegoDeCartas.Characters
{
    public static class CharacterRunState
    {
        public static CharacterData SelectedCharacter { get; private set; }
        public static SubclassData SelectedSubclass { get; private set; }

        public static bool HasCharacter => SelectedCharacter != null;
        public static bool HasSubclass => SelectedSubclass != null;

        public static void Select(CharacterData character)
        {
            SelectedCharacter = character;
            SelectedSubclass = null;
        }

        public static bool SelectSubclass(SubclassData subclass)
        {
            if (SelectedSubclass != null ||
                SelectedCharacter == null ||
                subclass == null ||
                subclass.character != SelectedCharacter ||
                SelectedCharacter.subclasses == null ||
                !SelectedCharacter.subclasses.Contains(subclass))
            {
                return false;
            }

            SelectedSubclass = subclass;
            return true;
        }

        public static void Clear()
        {
            SelectedCharacter = null;
            SelectedSubclass = null;
        }
    }
}
