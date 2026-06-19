using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using JuegoDeCartas.UI;

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
        public TextMeshProUGUI enemyNameText;
        public Image enemyImage;
        public Animator enemyAnimator;
        public CombatInfoPanelUI combatInfoPanel;

        [Header("Value Feedback")]
        [Min(0.01f)] public float valuePulseDuration = 0.28f;
        [Min(1f)] public float valuePulseScale = 1.14f;
        public Color positiveValueColor = new Color(0.35f, 1f, 0.45f, 1f);
        public Color negativeValueColor = new Color(1f, 0.32f, 0.28f, 1f);
        public AnimationCurve valuePulseCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        BattleManager battle;
        int prevHealth;
        int prevMaxHealth;
        int prevMana;
        int prevMaxMana;
        int prevArmor;
        int prevEnemyHealth;
        int prevEnemyMaxHealth;
        Coroutine hpPulse;
        Coroutine manaPulse;
        Coroutine armorPulse;
        Coroutine enemyHpPulse;
        readonly Dictionary<Transform, Vector3> baseScales = new Dictionary<Transform, Vector3>();
        readonly Dictionary<TextMeshProUGUI, Color> baseColors = new Dictionary<TextMeshProUGUI, Color>();

        public void Init(BattleManager battleManager)
        {
            battle = battleManager;

            if (combatInfoPanel != null)
                combatInfoPanel.Init(battle);
        }

        public void Refresh()
        {
            if (battle == null || battle.player == null)
                return;

            Entity player = battle.player;

            if (playerHpText != null)
                playerHpText.text = player.stats.health + " / " + player.stats.maxHealth;
            if (playerManaText != null)
                playerManaText.text = player.stats.mana + " / " + player.stats.maxMana;
            if (playerArmorText != null)
                playerArmorText.text = player.stats.armor.ToString();

            if (player.stats.health != prevHealth || player.stats.maxHealth != prevMaxHealth)
                RestartPulse(playerHpText, player.stats.health - prevHealth, ref hpPulse);
            if (player.stats.mana != prevMana || player.stats.maxMana != prevMaxMana)
                RestartPulse(playerManaText, player.stats.mana - prevMana, ref manaPulse);
            if (player.stats.armor != prevArmor)
                RestartPulse(playerArmorText, player.stats.armor - prevArmor, ref armorPulse);

            prevHealth = player.stats.health;
            prevMaxHealth = player.stats.maxHealth;
            prevMana = player.stats.mana;
            prevMaxMana = player.stats.maxMana;
            prevArmor = player.stats.armor;

            if (battle.enemy != null)
            {
                if (enemyHpText != null)
                    enemyHpText.text = battle.enemy.stats.health + " / " + battle.enemy.stats.maxHealth;

                if (battle.enemy.stats.health != prevEnemyHealth ||
                    battle.enemy.stats.maxHealth != prevEnemyMaxHealth)
                {
                    RestartPulse(
                        enemyHpText,
                        battle.enemy.stats.health - prevEnemyHealth,
                        ref enemyHpPulse
                    );
                }

                prevEnemyHealth = battle.enemy.stats.health;
                prevEnemyMaxHealth = battle.enemy.stats.maxHealth;
            }
            else
            {
                if (enemyHpText != null)
                    enemyHpText.text = "-";
                if (enemyNameText != null)
                    enemyNameText.text = string.Empty;
                prevEnemyHealth = -1;
                prevEnemyMaxHealth = -1;
            }

            if (combatInfoPanel != null)
                combatInfoPanel.Refresh();
        }

        void RestartPulse(TextMeshProUGUI text, int delta, ref Coroutine routine)
        {
            if (text == null)
                return;

            ResetVisual(text);
            if (routine != null)
                StopCoroutine(routine);
            routine = StartCoroutine(PulseText(text, delta));
        }

        IEnumerator PulseText(TextMeshProUGUI text, int delta)
        {
            Transform target = text.transform;
            CacheVisual(text);
            Vector3 baseScale = baseScales[target];
            Color baseColor = baseColors[text];
            Color feedbackColor = delta > 0
                ? positiveValueColor
                : delta < 0 ? negativeValueColor : baseColor;

            float elapsed = 0f;
            while (elapsed < valuePulseDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(elapsed / valuePulseDuration);
                float pulse = Mathf.Sin(progress * Mathf.PI);
                float curved = valuePulseCurve.Evaluate(progress);
                target.localScale = baseScale * Mathf.Lerp(1f, valuePulseScale, pulse);
                text.color = Color.Lerp(feedbackColor, baseColor, curved);
                yield return null;
            }

            ResetVisual(text);
        }

        void CacheVisual(TextMeshProUGUI text)
        {
            if (!baseScales.ContainsKey(text.transform))
                baseScales[text.transform] = text.transform.localScale;
            if (!baseColors.ContainsKey(text))
                baseColors[text] = text.color;
        }

        void ResetVisual(TextMeshProUGUI text)
        {
            if (text == null)
                return;

            CacheVisual(text);
            text.transform.localScale = baseScales[text.transform];
            text.color = baseColors[text];
        }

        public void SetEnemyVisual(
            string enemyName,
            Sprite sprite,
            RuntimeAnimatorController animatorController)
        {
            if (enemyNameText != null)
                enemyNameText.text = enemyName;

            if (enemyImage == null)
                return;

            enemyImage.sprite = sprite;
            enemyImage.enabled = sprite != null;

            if (enemyAnimator == null)
                return;

            enemyAnimator.runtimeAnimatorController = animatorController;
            enemyAnimator.enabled = animatorController != null;
        }
    }
}
