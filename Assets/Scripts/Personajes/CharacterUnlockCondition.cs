using UnityEngine;

namespace JuegoDeCartas.Characters
{
    public abstract class CharacterUnlockCondition : ScriptableObject
    {
        [TextArea] public string lockedMessage;

        public abstract bool IsMet();
    }
}
