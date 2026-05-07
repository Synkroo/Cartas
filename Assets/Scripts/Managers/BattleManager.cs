using System.Collections;
using UnityEngine;

public class BattleManager : MonoBehaviour
{
    public Entity player;
    public Entity enemy;

    public DeckManager deckManager;

    public Transform handParent;
    public GameObject cardPrefab;

    public UIManager uiManager;

    void Start()
    {
        player.Initialize();
        enemy.Initialize();

        deckManager.InitializeDeck();

        uiManager.Init(this);

        StartTurn();
    }

    public void StartTurn()
    {
        player.stats.mana = player.stats.maxMana;

        deckManager.DrawCards(4);

        RenderHand();
        UpdateUI();
    }

    public void DrawCards(int amount)
    {
        deckManager.DrawCards(amount);

        RenderHand();
        UpdateUI();
    }

    public void PlayCard(Card card)
    {
        if (player.stats.mana < card.data.cost)
            return;

        player.stats.mana -= card.data.cost;

        foreach (var effect in card.data.effects)
        {
            effect.Apply(this);
        }

        deckManager.hand.Remove(card);
        deckManager.discard.Add(card);

        RenderHand();
        UpdateUI();
    }

    void RenderHand()
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

            CardView view = obj.GetComponent<CardView>();
            view.Setup(card, this);
        }

        yield return null;
    }

    public void UpdateUI()
    {
        uiManager.Refresh();
    }

    public void EnemyTurn()
    {
        int damage = 10;

        int finalDamage = Mathf.Max(0, damage - player.stats.armor);

        player.stats.health -= finalDamage;
        player.stats.Clamp();

        UpdateUI();
    }
}