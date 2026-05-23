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

            if (battle.statsTracker != null)
                battle.statsTracker.RegisterTurnPlayed();

            UpdateTurnUI();

            var player = battle.player;

            if (battle.statsTracker != null)
                battle.statsTracker.RegisterMaxArmor(player.stats.armor);

            player.stats.mana = player.stats.maxMana;
            player.stats.armor = 0;

            if (battle.armorPerTurn > 0)
                player.stats.armor += battle.armorPerTurn;

            battle.deckManager.DrawStartingHand();
            battle.RenderHand();
            battle.UpdateUI();
        }

        public bool isExecuting;

        public void EndPlayerTurn()
        {
            if (currentTurn != Turn.Player || isExecuting)
                return;

            isExecuting = true;
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

            if (battle.statsTracker != null)
                battle.statsTracker.RegisterMaxEnemyDamage(pendingEnemyDamage);

            if (enemyNextAttackText != null)
                enemyNextAttackText.text = pendingEnemyDamage + " DMG";

            yield return new WaitForSeconds(0.5f);

            ExecuteEnemyAttack();

            yield return new WaitForSeconds(0.5f);

            isExecuting = false;
            StartPlayerTurn();
        }

        void ExecuteEnemyAttack()
        {
            battle.DamagePlayer(pendingEnemyDamage);
            battle.UpdateUI();
        }

        public void NextRound()
        {
            roundCount++;
            turnCount = 0;

            if (battle.statsTracker != null)
                battle.statsTracker.RegisterRoundPlayed();

            if (battle.regenPerRound > 0)
            {
                battle.player.stats.health =
                    Mathf.Min(battle.player.stats.health + battle.regenPerRound, battle.player.stats.maxHealth);
                battle.UpdateUI();
            }

            UpdateRoundUI();
        }

        void UpdateTurnUI()
        {
            if (turnText != null)
            {
                turnText.text = "Turno " + turnCount;
                turnText.transform.localScale = Vector3.zero;
                StartCoroutine(PopIn(turnText.transform));
            }
        }

        void UpdateRoundUI()
        {
            if (roundText != null)
            {
                roundText.text = "Ronda " + roundCount;
                roundText.transform.localScale = Vector3.zero;
                StartCoroutine(PopIn(roundText.transform));
            }
        }

        IEnumerator PopIn(Transform t)
        {
            float dur = 0.25f;
            float elapsed = 0;
            while (elapsed < dur)
            {
                float p = elapsed / dur;
                float s = Mathf.Sin(p * Mathf.PI * 0.5f);
                t.localScale = Vector3.one * s;
                elapsed += Time.deltaTime;
                yield return null;
            }
            t.localScale = Vector3.one;
        }
    }
}
