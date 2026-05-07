using TMPro;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    [Header("UI")]
    public TextMeshProUGUI playerHpText;
    public TextMeshProUGUI playerManaText;
    public TextMeshProUGUI playerArmorText;

    public TextMeshProUGUI enemyHpText;

    private BattleManager battle;

    public void Init(BattleManager battleManager)
    {
        battle = battleManager;
    }

    public void Refresh()
    {
        var player = battle.player;
        var enemy = battle.enemy;

        playerHpText.text =
            player.stats.health + " / " + player.stats.maxHealth;

        playerManaText.text =
            player.stats.mana + " / " + player.stats.maxMana;

        playerArmorText.text =
            player.stats.armor.ToString();

        enemyHpText.text =
            enemy.stats.health + " / " + enemy.stats.maxHealth;
    }
}