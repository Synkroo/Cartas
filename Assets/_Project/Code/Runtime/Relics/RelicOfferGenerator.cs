using System.Collections.Generic;
using JuegoDeCartas.Challenges;

namespace JuegoDeCartas.Relics
{
    public static class RelicOfferGenerator
    {
        public static List<RelicData> Roll(
            IReadOnlyList<RelicData> pool,
            IReadOnlyList<RelicData> owned,
            int count,
            IReadOnlyList<RelicData> excluded = null)
        {
            var candidates = new List<RelicData>();
            if (pool != null)
            {
                for (int i = 0; i < pool.Count; i++)
                {
                    RelicData relic = pool[i];
                    if (relic == null ||
                        relic.appearanceWeight <= 0f ||
                        Contains(owned, relic) ||
                        Contains(excluded, relic) ||
                        candidates.Contains(relic))
                    {
                        continue;
                    }

                    candidates.Add(relic);
                }
            }

            var result = new List<RelicData>();
            int targetCount = System.Math.Max(0, count);
            while (result.Count < targetCount && candidates.Count > 0)
            {
                int index = RollWeightedIndex(candidates);
                result.Add(candidates[index]);
                candidates.RemoveAt(index);
            }

            if (result.Count < targetCount && excluded != null)
            {
                List<RelicData> fallback = Roll(pool, owned, targetCount, null);
                for (int i = 0; i < fallback.Count && result.Count < targetCount; i++)
                {
                    if (!result.Contains(fallback[i]))
                        result.Add(fallback[i]);
                }
            }

            return result;
        }

        static int RollWeightedIndex(List<RelicData> candidates)
        {
            double totalWeight = 0d;
            for (int i = 0; i < candidates.Count; i++)
                totalWeight += candidates[i].appearanceWeight;

            if (totalWeight <= 0d)
                return 0;

            double roll = RunRandom.NextDouble() * totalWeight;
            double cumulative = 0d;
            for (int i = 0; i < candidates.Count; i++)
            {
                cumulative += candidates[i].appearanceWeight;
                if (roll <= cumulative)
                    return i;
            }

            return candidates.Count - 1;
        }

        static bool Contains(IReadOnlyList<RelicData> list, RelicData relic)
        {
            if (list == null)
                return false;

            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] == relic)
                    return true;
            }

            return false;
        }
    }
}
