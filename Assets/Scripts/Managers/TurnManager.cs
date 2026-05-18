using System.Collections;
using TMPro;
using UnityEngine;

namespace JuegoDeCartas.Managers
{
    public class TurnManager : MonoBehaviour
    {
        public enum Turn
        {
            Player,
            Enemy
        }

        [Header("References")]
        public BattleManager battle;

        [Header("UI")]
        public TextMeshProUGUI turnText;
        public TextMeshProUGUI roundText;
        public TextMeshProUGUI enemyNextAttackText;

        [Header("State")]
        public Turn currentTurn;

        public int turnCount = 0;
        public int roundCount = 1;

        private int pendingEnemyDamage;

        public void StartGame()
        {
            UpdateRoundUI();
            StartPlayerTurn();
        }

        public void StartPlayerTurn()
        {
            currentTurn = Turn.Player;

            turnCount++;

            UpdateTurnUI();

            var player = battle.player;
            player.stats.mana = player.stats.maxMana;
            player.stats.armor = 0;

            battle.deckManager.DrawStartingHand();
            battle.RenderHand();
            battle.UpdateUI();
        }

        public void EndPlayerTurn()
        {
            if (currentTurn != Turn.Player)
                return;

            currentTurn = Turn.Enemy;

            battle.deckManager.DiscardHand();

            battle.RenderHand();

            StartCoroutine(EnemyTurnRoutine());
        }

        IEnumerator EnemyTurnRoutine()
        {
            currentTurn = Turn.Enemy;

            yield return new WaitForSeconds(0.5f);

            if (battle.enemy == null)
            {
                StartPlayerTurn();
                yield break;
            }

            pendingEnemyDamage = Random.Range(
                battle.enemy.data.minDamage + battle.enemy.damageModifier,
                battle.enemy.data.maxDamage + battle.enemy.damageModifier + 1
            );
            pendingEnemyDamage = Mathf.Max(0, pendingEnemyDamage);

            if (enemyNextAttackText != null)
                enemyNextAttackText.text = pendingEnemyDamage + " DMG";

            yield return new WaitForSeconds(0.5f);

            ExecuteEnemyAttack();

            yield return new WaitForSeconds(0.5f);

            NextRound();

            StartPlayerTurn();
        }

        void ExecuteEnemyAttack()
        {
            ApplyDamageToPlayer(pendingEnemyDamage);
            battle.UpdateUI();
        }

        void ApplyDamageToPlayer(int damage)
        {
            var player = battle.player;

            int remaining = damage;

            if (player.stats.armor > 0)
            {
                int absorbed = Mathf.Min(player.stats.armor, remaining);
                player.stats.armor -= absorbed;
                remaining -= absorbed;
            }

            player.stats.health -= remaining;
            player.stats.Clamp();
        }

        public void NextRound()
        {
            roundCount++;
            turnCount = 0;
            UpdateRoundUI();
        }

        public void DiscardHand()
        {
            battle.deckManager.DiscardHand();
            battle.RenderHand();
        }

        void UpdateTurnUI()
        {
            if (turnText != null)
                turnText.text = "Turno " + turnCount;
        }

        void UpdateRoundUI()
        {
            if (roundText != null)
                roundText.text = "Ronda " + roundCount;
        }
    }
}
