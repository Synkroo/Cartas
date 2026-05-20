using TMPro;
using UnityEngine;

namespace JuegoDeCartas.Stats
{
    public class GameStatsTracker : MonoBehaviour
    {
        [Header("Stats Texts")]
        public TextMeshProUGUI victoryStatsText;
        public TextMeshProUGUI defeatStatsText;

        [Header("Stats")]
        public int turnsPlayed;
        public int roundsPlayed;
        public int enemiesDefeated;
        public int maxEnemyDamage;
        public int totalDamageDealt;
        public int totalDamageReceived;
        public int totalArmorGained;
        public int maxArmor;
        public int cardsUsed;
        public int maxCardDamage;

        public void RegisterTurnPlayed() { turnsPlayed++; }
        public void RegisterRoundPlayed() { roundsPlayed++; }
        public void RegisterEnemyDefeated() { enemiesDefeated++; }
        public void RegisterMaxEnemyDamage(int dmg) { if (dmg > maxEnemyDamage) maxEnemyDamage = dmg; }
        public void RegisterDamageDealt(int dmg) { totalDamageDealt += dmg; }
        public void RegisterDamageReceived(int dmg) { totalDamageReceived += dmg; }
        public void RegisterArmorGained(int armor) { totalArmorGained += armor; }
        public void RegisterMaxArmor(int armor) { if (armor > maxArmor) maxArmor = armor; }
        public void RegisterCardPlayed(int damageFromCard = 0) { cardsUsed++; if (damageFromCard > maxCardDamage) maxCardDamage = damageFromCard; }

        public string GetStatsText()
        {
            return $"Turnos jugados: {turnsPlayed}\t\tRondas jugadas: {roundsPlayed}\n\n" +
                   $"Enemigos derrotados: {enemiesDefeated}\t\tDaño maximo enemigo: {maxEnemyDamage}\n\n" +
                   $"Daño Realizado: {totalDamageDealt}\t\tDaño Recibido: {totalDamageReceived}\n\n" +
                   $"Armadura Total: {totalArmorGained}\t\tArmadura Maxima: {maxArmor}\n\n" +
                   $"Cartas Usadas: {cardsUsed}\t\tDaño maximo cartas: {maxCardDamage}";
        }

        public void PopulateStatsText()
        {
            string text = GetStatsText();
            if (victoryStatsText != null) victoryStatsText.text = text;
            if (defeatStatsText != null) defeatStatsText.text = text;
        }
    }
}
