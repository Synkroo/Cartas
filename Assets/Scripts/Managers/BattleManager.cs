using System.Collections.Generic;
using UnityEngine;
using JuegoDeCartas.Cards;
using JuegoDeCartas.Enemies;
using JuegoDeCartas.Missions;
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

            ApplySelectedMission();

            waveManager.uiManager = uiManager;
            waveManager.gameManager = gameManager;
            waveManager.statsTracker = statsTracker;
            waveManager.enemyHealthBar = GetEnemyHealthBar();
            waveManager.battleManager = this;

            waveManager.OnWaveCleared += OnWaveCleared;
            waveManager.OnEnemyDefeated += OnEnemyDefeated;

            waveManager.Initialize();

            deckManager.InitializeDeck();

            turnManager.StartGame();
        }

        void ApplySelectedMission()
        {
            MissionData mission = MissionRunState.SelectedMission;
            if (mission == null)
                return;

            waveManager.normalEnemies.Clear();
            waveManager.miniBossEnemies.Clear();

            for (int i = 0; i < mission.possibleNormalEnemies.Count; i++)
            {
                if (mission.possibleNormalEnemies[i] != null)
                    waveManager.normalEnemies.Add(mission.possibleNormalEnemies[i]);
            }

            for (int i = 0; i < mission.possibleMiniBosses.Count; i++)
            {
                if (mission.possibleMiniBosses[i] != null)
                    waveManager.miniBossEnemies.Add(mission.possibleMiniBosses[i]);
            }

            waveManager.finalBoss = mission.boss;
            waveManager.totalCombats = Mathf.Max(1, mission.combatCount);
            waveManager.miniBossFrequency = Mathf.Max(1, mission.miniBossFrequency);
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

            MissionData mission = MissionRunState.SelectedMission;
            if (mission != null)
                mission.MarkCompleted(MissionRunState.SelectedDifficulty);

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

            int cost = card.effectiveCost;
            if (player.stats.mana < cost)
                return;

            player.stats.mana -= cost;

            deckManager.hand.Remove(card);

            if (!card.data.destroyOnUse)
                deckManager.discard.Add(card);

            int repeats = 1 + card.reactivationCount;
            int totalDamage = 0;

            for (int i = 0; i < repeats; i++)
            {
                lastCardDamageDealt = 0;

                foreach (var effect in card.data.effects)
                {
                    if (effect != null)
                        effect.Apply(this);
                }

                totalDamage += lastCardDamageDealt;
            }

            if (statsTracker != null)
                statsTracker.RegisterCardPlayed(totalDamage);

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
