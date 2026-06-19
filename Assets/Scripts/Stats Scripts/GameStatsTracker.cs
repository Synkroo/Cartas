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
        public int totalInterestEarned;

        public void RegisterTurnPlayed() { turnsPlayed++; }
        public void RegisterRoundPlayed() { roundsPlayed++; }
        public void RegisterEnemyDefeated() { enemiesDefeated++; }
        public void RegisterMaxEnemyDamage(int damage) { if (damage > maxEnemyDamage) maxEnemyDamage = damage; }
        public void RegisterDamageDealt(int damage) { totalDamageDealt += damage; }
        public void RegisterDamageReceived(int damage) { totalDamageReceived += damage; }
        public void RegisterArmorGained(int armor) { totalArmorGained += armor; }
        public void RegisterMaxArmor(int armor) { if (armor > maxArmor) maxArmor = armor; }
        public void RegisterCardPlayed(int damageFromCard = 0)
        {
            cardsUsed++;
            if (damageFromCard > maxCardDamage)
                maxCardDamage = damageFromCard;
        }

        public void RegisterInterestEarned(int amount)
        {
            totalInterestEarned += Mathf.Max(0, amount);
        }

        public string GetStatsText()
        {
            return $"Turnos jugados: {turnsPlayed}\t\tRondas jugadas: {roundsPlayed}\n\n" +
                   $"Enemigos derrotados: {enemiesDefeated}\t\tDano maximo enemigo: {maxEnemyDamage}\n\n" +
                   $"Dano realizado: {totalDamageDealt}\t\tDano recibido: {totalDamageReceived}\n\n" +
                   $"Armadura total: {totalArmorGained}\t\tArmadura maxima: {maxArmor}\n\n" +
                   $"Cartas usadas: {cardsUsed}\t\tDano maximo cartas: {maxCardDamage}\n\n" +
                   $"Intereses obtenidos: {totalInterestEarned}";
        }

        public void PopulateStatsText()
        {
            string text = GetStatsText();
            if (victoryStatsText != null)
                victoryStatsText.text = text;
            if (defeatStatsText != null)
                defeatStatsText.text = text;
        }
    }
}
