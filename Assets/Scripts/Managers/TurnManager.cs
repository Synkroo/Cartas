using System.Collections;
using System.Collections.Generic;
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
        public TextMeshProUGUI enemyNextAttackShadowText;

        [Header("State")]
        public Turn currentTurn;

        public int turnCount = 0;
        public int roundCount = 1;

        [Header("Animation")]
        public AnimationCurve popInCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        private int pendingEnemyDamage;
        private object previewEnemyReference;
        private int previewDamageModifier;
        private int previewMinDamage;
        private int previewMaxDamage;
        private int previewTurnsSurvived;

        public void StartGame()
        {
            if (battle == null || battle.IsBattleEnded)
                return;

            UpdateRoundUI();
            StartPlayerTurn();
        }

        public void StartPlayerTurn()
        {
            if (battle == null || battle.IsBattleEnded || battle.player == null || battle.deckManager == null)
                return;

            currentTurn = Turn.Player;

            turnCount++;

            if (battle.statsTracker != null)
                battle.statsTracker.RegisterTurnPlayed();

            UpdateTurnUI();

            var player = battle.player;

            if (battle.statsTracker != null)
                battle.statsTracker.RegisterMaxArmor(player.stats.armor);

            player.stats.mana = player.stats.maxMana;
            player.stats.armor = battle.GetArmorAtPlayerTurnStart(player.stats.armor);

            if (battle.armorPerTurn > 0)
                battle.GainPlayerArmor(battle.armorPerTurn);
            if (battle.relicInventory != null)
                battle.relicInventory.OnPlayerTurnStarted();

            battle.deckManager.DrawStartingHand();
            battle.RenderHand();
            RefreshEnemyIntentPreview(true);
            battle.UpdateUI();
        }

        public bool isExecuting;

        public void EndPlayerTurn()
        {
            if (battle == null || battle.IsBattleEnded || battle.deckManager == null || currentTurn != Turn.Player || isExecuting)
                return;

            battle.AdvancePlayerDamageBonusTurn();
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

            if (battle == null || battle.IsBattleEnded)
            {
                isExecuting = false;
                yield break;
            }

            if (battle.enemy == null)
            {
                isExecuting = false;
                StartPlayerTurn();
                yield break;
            }

            battle.enemy.BeginTurn();
            battle.UpdateUI();

            yield return new WaitForSeconds(0.5f);

            ExecuteEnemyAttack();

            yield return new WaitForSeconds(0.5f);

            isExecuting = false;
            if (battle == null || battle.IsBattleEnded)
                yield break;

            StartPlayerTurn();
        }

        void ExecuteEnemyAttack()
        {
            if (battle == null || battle.IsBattleEnded)
                return;

            battle.DamagePlayer(pendingEnemyDamage);
            battle.UpdateUI();
        }

        public void NextRound()
        {
            if (battle == null || battle.IsBattleEnded || battle.player == null)
                return;

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
                float s = popInCurve.Evaluate(p);
                t.localScale = Vector3.one * s;
                elapsed += Time.deltaTime;
                yield return null;
            }
            t.localScale = Vector3.one;
        }

        public void RefreshEnemyIntentPreview(bool force = false)
        {
            if (battle == null || battle.enemy == null)
            {
                pendingEnemyDamage = 0;
                previewEnemyReference = null;
                SetEnemyIntentText("-");
                return;
            }

            if (currentTurn != Turn.Player || isExecuting)
                return;

            if (!force && PreviewStateMatchesCurrentEnemy())
            {
                SetEnemyIntentText(BuildEnemyIntentText());
                return;
            }

            pendingEnemyDamage = Mathf.Max(0, battle.enemy.RollProjectedNextAttackDamage());
            CachePreviewState();

            if (battle.statsTracker != null)
                battle.statsTracker.RegisterMaxEnemyDamage(pendingEnemyDamage);

            SetEnemyIntentText(BuildEnemyIntentText());
        }

        void SetEnemyIntentText(string text)
        {
            if (enemyNextAttackText != null)
                enemyNextAttackText.text = text;

            if (enemyNextAttackShadowText != null)
                enemyNextAttackShadowText.text = text;
        }

        string BuildEnemyIntentText()
        {
            if (battle == null || battle.enemy == null)
                return "-";

            var parts = new List<string>
            {
                pendingEnemyDamage + " de dano"
            };

            int nextArmor = Mathf.Max(0, battle.enemy.GetProjectedNextTurnArmorGain());

            if (nextArmor > 0)
                parts.Add(nextArmor + " de armadura");

            return string.Join(" ", parts);
        }

        bool PreviewStateMatchesCurrentEnemy()
        {
            return ReferenceEquals(previewEnemyReference, battle.enemy) &&
                   previewDamageModifier == battle.enemy.damageModifier &&
                   previewMinDamage == battle.enemy.currentMinDamage &&
                   previewMaxDamage == battle.enemy.currentMaxDamage &&
                   previewTurnsSurvived == battle.enemy.turnsSurvived;
        }

        void CachePreviewState()
        {
            previewEnemyReference = battle.enemy;
            previewDamageModifier = battle.enemy.damageModifier;
            previewMinDamage = battle.enemy.currentMinDamage;
            previewMaxDamage = battle.enemy.currentMaxDamage;
            previewTurnsSurvived = battle.enemy.turnsSurvived;
        }
    }
}
