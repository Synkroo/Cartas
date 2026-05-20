using System.Collections.Generic;
using UnityEngine;

namespace JuegoDeCartas.UI
{
    public class HandLayout : MonoBehaviour
    {
        public float baseSpacing = 100f;
        public float falloff = 0.15f;

        List<RectTransform> cards = new List<RectTransform>();
        bool dirty = true;

        void LateUpdate()
        {
            if (!dirty) return;
            dirty = false;

            RebuildList();

            int count = cards.Count;
            if (count == 0) return;

            float spacing = CalculateSpacing(count);
            float startX = -((count - 1) * spacing) / 2f;

            for (int i = 0; i < count; i++)
            {
                RectTransform card = cards[i];

                Vector2 targetPos = new Vector2(
                    startX + i * spacing,
                    0
                );

                card.anchoredPosition = targetPos;
            }
        }

        public void MarkDirty()
        {
            dirty = true;
        }

        void RebuildList()
        {
            cards.Clear();

            for (int i = 0; i < transform.childCount; i++)
            {
                RectTransform rt =
                    transform.GetChild(i).GetComponent<RectTransform>();

                if (rt != null)
                    cards.Add(rt);
            }
        }

        float CalculateSpacing(int count)
        {
            if (count <= 1)
                return 0f;

            if (count <= 5)
                return baseSpacing;

            float x = count - 5f;

            return baseSpacing / (1f + falloff * x);
        }
    }
}
