using System.Collections.Generic;
using JuegoDeCartas.Relics;
using UnityEngine;

namespace JuegoDeCartas.UI
{
    public class RelicInventoryUI : MonoBehaviour
    {
        public List<RelicIconUI> slots = new List<RelicIconUI>();
        public UITransitionAnimator transition;
        [Min(0f)] public float entranceDelay = 0.12f;

        void Start()
        {
            if (transition != null)
                transition.PlayIn(entranceDelay);
        }

        public void Refresh(IReadOnlyList<RelicData> relics)
        {
            for (int i = 0; i < slots.Count; i++)
            {
                if (slots[i] == null)
                    continue;

                RelicData relic = relics != null && i < relics.Count
                    ? relics[i]
                    : null;
                slots[i].Setup(relic);
            }
        }
    }
}
