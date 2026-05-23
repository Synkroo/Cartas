using System.Collections;
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
        private int prevHealth, prevMaxHealth, prevMana, prevMaxMana, prevArmor;
        private int prevEnemyHealth, prevEnemyMaxHealth;
        private Coroutine hpPulse, manaPulse, armorPulse, enemyHpPulse;

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

            if (player.stats.health != prevHealth || player.stats.maxHealth != prevMaxHealth)
            {
                if (hpPulse != null) StopCoroutine(hpPulse);
                hpPulse = StartCoroutine(PulseText(playerHpText.transform));
            }
            if (player.stats.mana != prevMana || player.stats.maxMana != prevMaxMana)
            {
                if (manaPulse != null) StopCoroutine(manaPulse);
                manaPulse = StartCoroutine(PulseText(playerManaText.transform));
            }
            if (player.stats.armor != prevArmor)
            {
                if (armorPulse != null) StopCoroutine(armorPulse);
                armorPulse = StartCoroutine(PulseText(playerArmorText.transform));
            }

            prevHealth = player.stats.health;
            prevMaxHealth = player.stats.maxHealth;
            prevMana = player.stats.mana;
            prevMaxMana = player.stats.maxMana;
            prevArmor = player.stats.armor;

            if (battle.enemy != null)
            {
                enemyHpText.text =
                    battle.enemy.stats.health + " / " +
                    battle.enemy.stats.maxHealth;

                if (battle.enemy.stats.health != prevEnemyHealth || battle.enemy.stats.maxHealth != prevEnemyMaxHealth)
                {
                    if (enemyHpPulse != null) StopCoroutine(enemyHpPulse);
                    enemyHpPulse = StartCoroutine(PulseText(enemyHpText.transform));
                }

                prevEnemyHealth = battle.enemy.stats.health;
                prevEnemyMaxHealth = battle.enemy.stats.maxHealth;
            }
            else
            {
                enemyHpText.text = "-";
                prevEnemyHealth = -1;
                prevEnemyMaxHealth = -1;
            }
        }

        IEnumerator PulseText(Transform t)
        {
            float dur = 0.2f;
            float elapsed = 0;
            while (elapsed < dur)
            {
                float p = elapsed / dur;
                float s = 1f + Mathf.Sin(p * Mathf.PI) * 0.12f;
                t.localScale = Vector3.one * s;
                elapsed += Time.deltaTime;
                yield return null;
            }
            t.localScale = Vector3.one;
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
