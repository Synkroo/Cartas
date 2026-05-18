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
            health = Mathf.Clamp(health, 0, maxHealth);
            mana = Mathf.Clamp(mana, 0, maxMana);
        }
    }
}
