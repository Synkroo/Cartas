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

        [Header("Systems")]
        public DeckManager deckManager;
        public TurnManager turnManager;
        public UIManager uiManager;
        public DeckViewerUI deckViewer;
        public GameManager gameManager;
        public JuegoDeCartas.Stats.GameStatsTracker statsTracker;

        [Header("Wave")]
        public WaveManager waveManager = new WaveManager();

        [Header("Hand")]
        public HandRenderer handRenderer = new HandRenderer();

        private int lastCardDamageDealt;

        [HideInInspector] public int armorPerTurn;
        [HideInInspector] public int regenPerRound;

        public Enemy enemy => waveManager.enemy;

        void OnDestroy()
        {
            if (deckManager != null)
                deckManager.OnDeckChanged -= RefreshDeckUI;

            waveManager.OnWaveCleared -= OnWaveCleared;
            waveManager.OnEnemyDefeated -= OnEnemyDefeated;
        }

        void Start()
        {
            uiManager.Init(this);

            deckManager.OnDeckChanged += RefreshDeckUI;

            waveManager.uiManager = uiManager;
            waveManager.gameManager = gameManager;
            waveManager.statsTracker = statsTracker;
            waveManager.enemyHealthBar = GetEnemyHealthBar();

            waveManager.OnWaveCleared += OnWaveCleared;
            waveManager.OnEnemyDefeated += OnEnemyDefeated;

            waveManager.Initialize();

            deckManager.InitializeDeck();

            turnManager.StartGame();
        }

        EnemyHealthBar GetEnemyHealthBar()
        {
            if (waveManager.enemyHealthBar != null)
                return waveManager.enemyHealthBar;

            var bar = GetComponentInChildren<EnemyHealthBar>();
            if (bar != null)
                return bar;

            return FindAnyObjectByType<EnemyHealthBar>();
        }

        void RefreshDeckUI()
        {
            if (deckViewer != null && deckViewer.panel.activeSelf)
                deckViewer.ShowRemaining();
        }

        void OnWaveCleared()
        {
            if (statsTracker != null)
                statsTracker.PopulateStatsText();
            if (gameManager != null)
                gameManager.ShowVictory();
        }

        void OnEnemyDefeated()
        {
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

        public void DamageEnemy(int damage)
        {
            if (enemy == null) return;

            lastCardDamageDealt += damage;

            if (statsTracker != null)
                statsTracker.RegisterDamageDealt(damage);

            waveManager.DamageEnemy(damage, out bool died);

            UpdateUI();
        }

        public void DamagePlayer(int damage)
        {
            if (player == null) return;

            int dealt = player.TakeDamage(damage);

            UpdateUI();

            if (statsTracker != null)
                statsTracker.RegisterDamageReceived(dealt);

            if (player.stats.health <= 0 && gameManager != null)
            {
                if (statsTracker != null)
                    statsTracker.PopulateStatsText();
                gameManager.ShowDefeat();
            }
        }

        public void ContinueAfterShop()
        {
            waveManager.SpawnNext();

            deckManager.deck.AddRange(deckManager.hand);
            deckManager.hand.Clear();
            deckManager.Shuffle(deckManager.deck);

            turnManager.NextRound();
            turnManager.StartPlayerTurn();
        }

        public void RenderHand()
        {
            if (handRenderer != null)
                handRenderer.Render(deckManager.hand, this);
        }

        public void UpdateUI()
        {
            uiManager.Refresh();
        }
    }
}
