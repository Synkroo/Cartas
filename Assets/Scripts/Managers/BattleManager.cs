using System.Collections.Generic;
using UnityEngine;
using JuegoDeCartas.Cards;
using JuegoDeCartas.Enemies;
using JuegoDeCartas.Missions;
using JuegoDeCartas.UI;
using JuegoDeCartas.Characters;
using JuegoDeCartas.Progression;

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
        private bool battleEnded;
        private bool runRecorded;

        [HideInInspector] public int armorPerTurn;
        [HideInInspector] public int regenPerRound;
        [HideInInspector] public int playerDamageBonus;
        [HideInInspector] public int playerDamageBonusTurnsRemaining;

        public Enemy enemy => waveManager.enemy;
        public bool IsBattleEnded => battleEnded;

        void OnDestroy()
        {
            if (deckManager != null)
                deckManager.OnDeckChanged -= RefreshDeckUI;

            waveManager.OnWaveCleared -= OnWaveCleared;
            waveManager.OnEnemyDefeated -= OnEnemyDefeated;
        }

        void Start()
        {
            ApplySelectedCharacter();
            CollectionProgress.RegisterRunStarted(CharacterRunState.SelectedCharacter);

            if (uiManager != null)
                uiManager.Init(this);

            if (deckManager != null)
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

            if (deckManager != null)
                deckManager.InitializeDeck();

            if (turnManager != null)
                turnManager.StartGame();
        }

        public void ApplySelectedCharacter()
        {
            CharacterData character = CharacterRunState.SelectedCharacter;
            if (character == null || player == null || deckManager == null)
                return;

            player.stats.maxHealth = Mathf.Max(1, character.maxHealth);
            player.stats.health = player.stats.maxHealth;
            player.stats.maxMana = Mathf.Max(0, character.maxMana);
            player.stats.mana = player.stats.maxMana;
            player.stats.armor = 0;
            deckManager.cardsPerTurn = Mathf.Max(0, character.cardsPerTurn);
            deckManager.startingDeck = new List<CardData>(character.startingDeck);

            if (gameManager != null)
                gameManager.dinero = Mathf.Max(0, character.startingGold);
        }

        void ApplySelectedMission()
        {
            MissionData mission = MissionRunState.SelectedMission;
            if (mission == null)
                return;

            waveManager.normalEnemies.Clear();
            waveManager.miniBossEnemies.Clear();

            if (mission.possibleNormalEnemies != null)
            {
                for (int i = 0; i < mission.possibleNormalEnemies.Count; i++)
                {
                    if (mission.possibleNormalEnemies[i] != null)
                        waveManager.normalEnemies.Add(mission.possibleNormalEnemies[i]);
                }
            }

            if (mission.possibleMiniBosses != null)
            {
                for (int i = 0; i < mission.possibleMiniBosses.Count; i++)
                {
                    if (mission.possibleMiniBosses[i] != null)
                        waveManager.miniBossEnemies.Add(mission.possibleMiniBosses[i]);
                }
            }

            waveManager.finalBoss = mission.boss;
            waveManager.totalCombats = Mathf.Max(1, mission.combatCount);
            waveManager.miniBossFrequency = Mathf.Max(1, mission.miniBossFrequency);
        }

        EnemyHealthBar GetEnemyHealthBar()
        {
            if (waveManager.enemyHealthBar != null)
                return waveManager.enemyHealthBar;

            var bars = FindObjectsByType<EnemyHealthBar>(FindObjectsInactive.Include);
            for (int i = 0; i < bars.Length; i++)
            {
                if (bars[i].target == EnemyHealthBar.HealthTarget.Enemy)
                    return bars[i];
            }

            return null;
        }

        void RefreshDeckUI()
        {
            if (deckViewer != null && deckViewer.panel != null && deckViewer.panel.activeSelf)
                deckViewer.ShowRemaining();
        }

        void OnWaveCleared()
        {
            battleEnded = true;

            if (statsTracker != null)
                statsTracker.PopulateStatsText();

            MissionData mission = MissionRunState.SelectedMission;
            if (mission != null)
                mission.MarkCompleted(
                    MissionRunState.SelectedDifficulty,
                    CharacterRunState.SelectedCharacter
                );

            RecordRun(true);

            if (gameManager != null)
                gameManager.ShowVictory();
        }

        void OnEnemyDefeated()
        {
        }

        public void DrawCards(int amount)
        {
            if (!battleEnded && deckManager != null)
                deckManager.DrawCards(amount);
        }

        public void PlayCard(Card card)
        {
            if (battleEnded || card == null || card.data == null || player == null || deckManager == null || turnManager == null)
                return;

            if (turnManager.currentTurn != TurnManager.Turn.Player || turnManager.isExecuting)
                return;

            int cost = card.effectiveCost;
            if (player.stats.mana < cost)
                return;

            player.stats.mana -= cost;
            CollectionProgress.MarkCardUsed(card.data);

            deckManager.hand.Remove(card);

            if (!card.effectiveDestroyOnUse)
                deckManager.discard.Add(card);

            int repeats = 1 + card.reactivationCount;
            int totalDamage = 0;

            foreach (var effect in card.data.effects)
            {
                if (effect == null)
                    continue;

                if (effect.UsesReactivationMultiplier)
                {
                    lastCardDamageDealt = 0;
                    effect.Apply(this, repeats);
                    totalDamage += lastCardDamageDealt;
                    continue;
                }

                for (int i = 0; i < repeats; i++)
                {
                    lastCardDamageDealt = 0;
                    effect.Apply(this);
                    totalDamage += lastCardDamageDealt;
                }
            }

            CardUpgradeOption upgrade = card.SelectedUpgrade;
            if (upgrade != null && upgrade.bonusEffects != null)
            {
                foreach (var effect in upgrade.bonusEffects)
                {
                    if (effect == null)
                        continue;

                    lastCardDamageDealt = 0;
                    effect.Apply(this);
                    totalDamage += lastCardDamageDealt;
                }
            }

            if (statsTracker != null)
                statsTracker.RegisterCardPlayed(totalDamage);

            RenderHand();
            UpdateUI();
            if (deckViewer != null && deckViewer.panel != null && deckViewer.panel.activeSelf)
                deckViewer.ShowDiscard();
        }

        public void DamageEnemy(int damage)
        {
            if (battleEnded || enemy == null) return;

            int totalDamage = Mathf.Max(0, damage + playerDamageBonus);

            lastCardDamageDealt += totalDamage;

            if (statsTracker != null)
                statsTracker.RegisterDamageDealt(totalDamage);

            waveManager.DamageEnemy(totalDamage, out bool died);

            UpdateUI();
        }

        public void DamagePlayer(int damage)
        {
            if (battleEnded || player == null) return;

            int dealt = player.TakeDamage(damage);

            UpdateUI();

            if (statsTracker != null)
                statsTracker.RegisterDamageReceived(dealt);

            if (player.stats.health <= 0 && gameManager != null)
            {
                battleEnded = true;
                if (statsTracker != null)
                    statsTracker.PopulateStatsText();
                RecordRun(false);
                gameManager.ShowDefeat();
            }
        }

        public void ContinueAfterShop()
        {
            if (battleEnded || deckManager == null || turnManager == null)
                return;

            waveManager.SpawnNext();

            deckManager.deck.AddRange(deckManager.hand);
            deckManager.hand.Clear();
            deckManager.Shuffle(deckManager.deck);

            turnManager.NextRound();
            turnManager.StartPlayerTurn();
        }

        public void ApplyPlayerDamageBonus(int amount, int turns)
        {
            playerDamageBonus = amount;
            playerDamageBonusTurnsRemaining = Mathf.Max(0, turns);
        }

        public void AdvancePlayerDamageBonusTurn()
        {
            if (playerDamageBonusTurnsRemaining <= 0)
                return;

            playerDamageBonusTurnsRemaining--;

            if (playerDamageBonusTurnsRemaining <= 0)
            {
                playerDamageBonusTurnsRemaining = 0;
                playerDamageBonus = 0;
            }
        }

        public void ResetTemporaryCombatEffects()
        {
            playerDamageBonus = 0;
            playerDamageBonusTurnsRemaining = 0;
        }

        public void RenderHand()
        {
            if (handRenderer != null && deckManager != null)
                handRenderer.Render(deckManager.hand, this);
        }

        public void UpdateUI()
        {
            if (uiManager != null)
                uiManager.Refresh();
            if (turnManager != null)
                turnManager.RefreshEnemyIntentPreview();
        }

        void RecordRun(bool completed)
        {
            if (runRecorded)
                return;

            runRecorded = true;
            CollectionProgress.RegisterRunFinished(
                CharacterRunState.SelectedCharacter,
                statsTracker,
                completed
            );
        }
    }
}
