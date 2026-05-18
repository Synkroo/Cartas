using UnityEngine;

namespace JuegoDeCartas.Enemies
{
    [CreateAssetMenu(fileName = "EnemyData", menuName = "Game/Enemy")]
    public class EnemyData : ScriptableObject
    {
        public string enemyName;

        [Header("Stats")]
        public int maxHealth = 30;

        [Header("Damage Range")]
        public int minDamage = 5;
        public int maxDamage = 10;

        [Header("Visual")]
        public Sprite sprite;
    }
}
