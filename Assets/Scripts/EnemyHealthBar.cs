using UnityEngine;
using UnityEngine.UI;
using JuegoDeCartas.Managers;

namespace JuegoDeCartas.Enemies
{
    public class EnemyHealthBar : MonoBehaviour
    {
        public enum HealthTarget
        {
            Enemy,
            Player,
            PlayerMana
        }

        public BattleManager battle;
        public Image fillImage;
        public float lerpSpeed = 10f;
        public HealthTarget target;
        public Image.FillMethod fillMethod = Image.FillMethod.Horizontal;
        public int fillOrigin;

        [Header("Enemy Bar Visuals")]
        public GameObject bossBarObject;
        public Image bossFillImage;
        public GameObject regularBarObject;
        public Image regularFillImage;

        Image activeFillImage;
        EnemyTier activeEnemyTier = EnemyTier.Normal;

        void Start()
        {
            ConfigureFillImage(fillImage);
            ConfigureFillImage(bossFillImage);
            ConfigureFillImage(regularFillImage);

            if (target == HealthTarget.Enemy && activeFillImage == null)
                SetEnemyTier(EnemyTier.Normal);
        }

        void Update()
        {
            Image currentFillImage = target == HealthTarget.Enemy
                ? activeFillImage
                : fillImage;

            if (battle == null || currentFillImage == null)
                return;

            var stats = target == HealthTarget.Enemy
                ? battle.enemy?.stats
                : battle.player?.stats;

            if (stats == null)
                return;

            float percent = target == HealthTarget.PlayerMana
                ? GetPercent(stats.mana, stats.maxMana)
                : GetPercent(stats.health, stats.maxHealth);

            currentFillImage.fillAmount =
                Mathf.Lerp(
                    currentFillImage.fillAmount,
                    percent,
                    Time.deltaTime * lerpSpeed
                );
        }

        static float GetPercent(int current, int maximum)
        {
            return maximum > 0 ? (float)current / maximum : 0f;
        }

        public void SetEnemyTier(EnemyTier enemyTier)
        {
            if (target != HealthTarget.Enemy)
                return;

            activeEnemyTier = enemyTier;
            bool useBossBar = enemyTier == EnemyTier.Boss;

            if (bossBarObject != null)
                bossBarObject.SetActive(useBossBar);

            if (regularBarObject != null)
                regularBarObject.SetActive(!useBossBar);

            activeFillImage = useBossBar ? bossFillImage : regularFillImage;
        }

        public void SetVisible(bool visible)
        {
            if (target != HealthTarget.Enemy)
            {
                enabled = visible;
                return;
            }

            if (!visible)
            {
                if (bossBarObject != null)
                    bossBarObject.SetActive(false);

                if (regularBarObject != null)
                    regularBarObject.SetActive(false);
            }
            else
            {
                SetEnemyTier(activeEnemyTier);
            }

            enabled = visible;
        }

        void ConfigureFillImage(Image image)
        {
            if (image == null)
                return;

            image.type = Image.Type.Filled;
            image.fillMethod = fillMethod;
            image.fillOrigin = fillOrigin;
        }
    }
}
