using UnityEngine;

namespace JuegoDeCartas.Stats
{
    [System.Serializable]
    public class Stats
    {
        public int health;
        public int maxHealth;

        public int mana;
        public int maxMana;

        public int armor;

        public void Clamp()
        {
            maxHealth = Mathf.Max(1, maxHealth);
            maxMana = Mathf.Max(0, maxMana);
            health = Mathf.Clamp(health, 0, maxHealth);
            mana = Mathf.Clamp(mana, 0, maxMana);
            armor = Mathf.Max(0, armor);
        }
    }
}
