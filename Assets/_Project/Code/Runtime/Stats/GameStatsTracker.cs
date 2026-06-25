using TMPro;
using UnityEngine;

namespace JuegoDeCartas.Stats
{
    public class GameStatsTracker : MonoBehaviour
    {
        [Header("Stats Texts")]
        public TextMeshProUGUI victoryStatsText;
        public TextMeshProUGUI defeatStatsText;
        public TextMeshProUGUI victorySecondaryStatsText;
        public TextMeshProUGUI defeatSecondaryStatsText;

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
            return GetPrimaryStatsText() + "\n\n" + GetSecondaryStatsText();
        }

        public string GetPrimaryStatsText()
        {
            return $"Turnos jugados: {turnsPlayed}\n\n" +
                   $"Enemigos derrotados: {enemiesDefeated}\n\n" +
                   $"Daño realizado: {totalDamageDealt}\n\n" +
                   $"Armadura total: {totalArmorGained}\n\n" +
                   $"Cartas usadas: {cardsUsed}\n\n" +
                   $"Intereses obtenidos: {totalInterestEarned}";
        }

        public string GetSecondaryStatsText()
        {
            return $"Rondas jugadas: {roundsPlayed}\n\n" +
                   $"Daño maximo enemigo: {maxEnemyDamage}\n\n" +
                   $"Daño recibido: {totalDamageReceived}\n\n" +
                   $"Armadura maxima: {maxArmor}\n\n" +
                   $"Daño maximo cartas: {maxCardDamage}";
        }

        public void PopulateStatsText()
        {
            string primary = GetPrimaryStatsText();
            string secondary = GetSecondaryStatsText();
            if (victoryStatsText != null)
                victoryStatsText.text = primary;
            if (defeatStatsText != null)
                defeatStatsText.text = primary;
            if (victorySecondaryStatsText != null)
                victorySecondaryStatsText.text = secondary;
            if (defeatSecondaryStatsText != null)
                defeatSecondaryStatsText.text = secondary;
        }
    }
}
