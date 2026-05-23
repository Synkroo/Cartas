using UnityEngine;
using UnityEngine.UI;
using JuegoDeCartas.Managers;

namespace JuegoDeCartas.Enemies
{
    public class EnemyHealthBar : MonoBehaviour
    {
        public BattleManager battle;

        public Image fillImage;

        public float lerpSpeed = 10f;

        void Start()
        {
            if (fillImage != null)
            {
                fillImage.type = Image.Type.Filled;
                fillImage.fillMethod = Image.FillMethod.Horizontal;
                fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
            }
        }

        void Update()
        {
            if (battle?.enemy == null)
                return;

            float percent = (float)battle.enemy.stats.health / battle.enemy.stats.maxHealth;

            fillImage.fillAmount =
                Mathf.Lerp(
                    fillImage.fillAmount,
                    percent,
                    Time.deltaTime * lerpSpeed
                );
        }
    }
}
