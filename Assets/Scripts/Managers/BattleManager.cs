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
        public EndGameMenu endGameMenu;

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

            turnManager.battle = this;
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
            UpdateUI();
        }

        public void PlayCard(Card card)
        {
            if (card == null) return;

            if (player.stats.mana < card.data.cost)
                return;

            player.stats.mana -= card.data.cost;

            deckManager.hand.Remove(card);
            deckManager.discard.Add(card);

            foreach (var effect in card.data.effects)
            {
                if (effect != null)
                    effect.Apply(this);
            }

            RenderHand();
            UpdateUI();
            deckViewer.ShowDiscard();
        }

        void SpawnEnemy()
        {
            if (currentEnemyIndex >= enemyWave.Count)
            {
                enemy = null;
                return;
            }

            enemy = new Enemy();
            enemy.Initialize(enemyWave[currentEnemyIndex]);
            currentEnemyIndex++;

            uiManager.SetEnemySprite(enemy.data.sprite);
            UpdateUI();
        }

        public void DamageEnemy(int damage)
        {
            if (enemy == null) return;

            enemy.currentHealth -= damage;

            if (enemy.currentHealth <= 0)
            {
                enemy.currentHealth = 0;
                EnemyDeath();
            }

            UpdateUI();
        }

        void EnemyDeath()
        {
            turnManager.NextRound();
            SpawnEnemy();

            if (enemy == null && endGameMenu != null)
                endGameMenu.ShowVictory();
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

            if (stats.health <= 0 && endGameMenu != null)
                endGameMenu.ShowDefeat();
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
        }

        public void UpdateUI()
        {
            uiManager.Refresh();
        }
    }
}
