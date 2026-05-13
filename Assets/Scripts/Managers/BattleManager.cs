using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

    [Header("Hand")]
    public Transform handParent;
    public GameObject cardPrefab;

    void Start()
    {
        player.Initialize();
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
        deckViewer?.ShowDiscard();
    }

    public void DrawCards(int amount)
    {
        deckManager.DrawCards(amount);
        RenderHand();
        UpdateUI();
    }

    public void PlayCard(Card card)
    {
        if (card == null) return;

        Card playedCard = card;

        if (player.stats.mana < playedCard.data.cost)
            return;

        player.stats.mana -= playedCard.data.cost;

        foreach (var effect in playedCard.data.effects)
        {
            if (effect != null)
                effect.Apply(this);
        }

        deckManager.hand.Remove(playedCard);
        deckManager.discard.Add(playedCard);

        RenderHand();
        UpdateUI();

        deckViewer.ShowDiscard();
    }

    void SpawnEnemy()
    {
        if (currentEnemyIndex >= enemyWave.Count)
        {
            Debug.Log("🏆 Has derrotado todos los enemigos");
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
        StopAllCoroutines();
        StartCoroutine(RenderRoutine());
    }

    IEnumerator RenderRoutine()
    {
        yield return null;

        foreach (Transform child in handParent)
            Destroy(child.gameObject);

        yield return null;

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