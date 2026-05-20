using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JuegoDeCartas.Managers
{
    public class UIManager : MonoBehaviour
    {
        [Header("Player UI")]
        public TextMeshProUGUI playerHpText;
        public TextMeshProUGUI playerManaText;
        public TextMeshProUGUI playerArmorText;

        [Header("Enemy UI")]
        public TextMeshProUGUI enemyHpText;

        public Image enemyImage;

        private BattleManager battle;

        public void Init(BattleManager battleManager)
        {
            battle = battleManager;
        }

        public void Refresh()
        {
            var player = battle.player;

            playerHpText.text =
                player.stats.health + " / " + player.stats.maxHealth;

            playerManaText.text =
                player.stats.mana + " / " + player.stats.maxMana;

            playerArmorText.text =
                player.stats.armor.ToString();

            if (battle.enemy != null)
            {
                enemyHpText.text =
                    battle.enemy.stats.health + " / " +
                    battle.enemy.stats.maxHealth;
            }
            else
            {
                enemyHpText.text = "-";
            }
        }

        public void SetEnemySprite(Sprite sprite)
        {
            if (enemyImage == null)
                return;

            enemyImage.sprite = sprite;

            enemyImage.enabled = sprite != null;
        }
    }
}
