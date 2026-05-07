using UnityEngine;

public class CardHover : MonoBehaviour
{
    public bool isHovering;

    public float hoverHeight = 40f;
    public float hoverSpeed = 12f;

    RectTransform rect;
    Vector2 basePosition;

    void Awake()
    {
        rect = GetComponent<RectTransform>();
    }

    public void SetBasePosition(Vector2 pos)
    {
        basePosition = pos;
    }

    void Update()
    {
        Vector2 target = basePosition;

        if (isHovering)
            target += Vector2.up * hoverHeight;

        rect.anchoredPosition = Vector2.Lerp(
            rect.anchoredPosition,
            target,
            Time.deltaTime * hoverSpeed
        );
    }

    public void SetHover(bool value)
    {
        isHovering = value;
    }
}