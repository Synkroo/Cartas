using UnityEngine;
using UnityEngine.UI;

public class EnemyHealthBar : MonoBehaviour
{
    public BattleManager battle;

    public Image fillImage;

    public float lerpSpeed = 10f;

    void Update()
    {
        if (battle.enemy == null)
            return;

        float current = battle.enemy.currentHealth;

        float max = battle.enemy.data.maxHealth;

        float percent = current / max;

        fillImage.fillAmount =
            Mathf.Lerp(
                fillImage.fillAmount,
                percent,
                Time.deltaTime * lerpSpeed
            );
    }
}