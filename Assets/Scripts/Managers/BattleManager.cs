using System.Collections.Generic;
using UnityEngine;
using JuegoDeCartas.Cards;
using JuegoDeCartas.Enemies;
using JuegoDeCartas.UI;

namespace JuegoDeCartas.Managers
{
    public class BattleManager : MonoBehaviour
    {
        [Header("Player")]
        public Entity player;

        [Header("Enemy")]
        public Enemy enemy;

        public List<EnemyData> enemyWave = new List<EnemyData>();
        private int currentEnemyIndex = 0;

        [Header("Systems")]
        public DeckManager deckManager;
        public TurnManager turnManager;
        public UIManager uiManager;
        public DeckViewerUI deckViewer;
        public GameManager gameManager;
        public JuegoDeCartas.Stats.GameStatsTracker statsTracker;
        public EnemyHealthBar enemyHealthBar;

        private int lastCardDamageDealt;

        [HideInInspector] public int armorPerTurn;
        [HideInInspector] public int regenPerRound;

        [Header("Hand")]
        public Transform handParent;
        public GameObject cardPrefab;

        void OnDestroy()
        {
            if (deckManager != null)
                deckManager.OnDeckChanged -= RefreshDeckUI;
        }

        void Start()
        {
            uiManager.Init(this);

            deckManager.OnDeckChanged += RefreshDeckUI;

            ShuffleEnemyWave();
            SpawnEnemy();

            deckManager.InitializeDeck();

            turnManager.StartGame();
        }

        void RefreshDeckUI()
        {
            if (deckViewer != null && deckViewer.panel.activeSelf)
                deckViewer.ShowRemaining();
        }

        public void DrawCards(int amount)
        {
            deckManager.DrawCards(amount);
        }

        public void PlayCard(Card card)
        {
            if (card == null) return;
            if (turnManager.currentTurn != TurnManager.Turn.Player || turnManager.isExecuting)
                return;

            if (player.stats.mana < card.data.cost)
                return;

            player.stats.mana -= card.data.cost;

            deckManager.hand.Remove(card);
            deckManager.discard.Add(card);

            lastCardDamageDealt = 0;

            foreach (var effect in card.data.effects)
            {
                if (effect != null)
                    effect.Apply(this);
            }

            if (statsTracker != null)
                statsTracker.RegisterCardPlayed(lastCardDamageDealt);

            RenderHand();
            UpdateUI();
            if (deckViewer != null) deckViewer.ShowDiscard();
        }

        void SpawnEnemy()
        {
            if (currentEnemyIndex >= enemyWave.Count)
            {
                enemy = null;
                if (enemyHealthBar != null)
                    enemyHealthBar.enabled = false;
                return;
            }

            enemy = new Enemy();
            enemy.Initialize(enemyWave[currentEnemyIndex]);
            currentEnemyIndex++;

            uiManager.SetEnemySprite(enemy.data.sprite);

            if (enemyHealthBar != null)
                enemyHealthBar.enabled = true;
        }

        public void DamageEnemy(int damage)
        {
            if (enemy == null) return;

            enemy.stats.health -= damage;
            lastCardDamageDealt += damage;

            if (statsTracker != null)
                statsTracker.RegisterDamageDealt(damage);

            if (enemy.stats.health <= 0)
            {
                enemy.stats.health = 0;
                EnemyDeath();
            }

            UpdateUI();
        }

        void EnemyDeath()
        {
            if (statsTracker != null)
                statsTracker.RegisterEnemyDefeated();

            if (enemy != null && enemy.data != null && gameManager != null)
                gameManager.dinero += enemy.data.isBoss ? 500 : 200;

            bool noMoreEnemies = currentEnemyIndex >= enemyWave.Count;

            if (noMoreEnemies)
            {
                if (enemyHealthBar != null)
                    enemyHealthBar.enabled = false;

                if (statsTracker != null)
                    statsTracker.PopulateStatsText();
                if (gameManager != null)
                    gameManager.ShowVictory();
                return;
            }

            if (gameManager != null)
                gameManager.OpenShop();
        }

        public void ContinueAfterShop()
        {
            SpawnEnemy();
            deckManager.deck.AddRange(deckManager.hand);
            deckManager.hand.Clear();
            deckManager.Shuffle(deckManager.deck);
            turnManager.NextRound();
            turnManager.StartPlayerTurn();
        }

        public void DamagePlayer(int damage)
        {
            if (player == null) return;

            var stats = player.stats;
            int remaining = damage;

            if (stats.armor > 0)
            {
                int absorbed = Mathf.Min(stats.armor, remaining);
                stats.armor -= absorbed;
                remaining -= absorbed;
            }

            stats.health -= remaining;
            stats.Clamp();

            UpdateUI();

            if (statsTracker != null)
                statsTracker.RegisterDamageReceived(remaining);

            if (stats.health <= 0 && gameManager != null)
            {
                if (statsTracker != null)
                    statsTracker.PopulateStatsText();
                gameManager.ShowDefeat();
            }
        }

        void ShuffleEnemyWave()
        {
            for (int i = 0; i < enemyWave.Count; i++)
            {
                EnemyData temp = enemyWave[i];
                int randomIndex = Random.Range(i, enemyWave.Count);
                enemyWave[i] = enemyWave[randomIndex];
                enemyWave[randomIndex] = temp;
            }
        }

        public void RenderHand()
        {
            foreach (Transform child in handParent)
                Destroy(child.gameObject);

            foreach (var card in deckManager.hand)
            {
                GameObject obj = Instantiate(cardPrefab, handParent);
                obj.GetComponent<CardView>().Setup(card, this);
            }

            HandLayout layout = handParent.GetComponentInParent<HandLayout>();
            if (layout != null)
                layout.MarkDirty();
        }

        public void UpdateUI()
        {
            uiManager.Refresh();
        }
    }
}
