using System;
using System.Collections.Generic;
using UnityEngine;

namespace JuegoDeCartas.Articulos
{
    public static class ItemPackGenerator
    {
        public static ItemPackOffer Generate(ItemPackData definition, List<ArticuloData> itemPool, int? seed = null)
        {
            var contents = new List<ArticuloData>();
            if (definition == null || itemPool == null)
                return new ItemPackOffer(definition, contents);

            var available = new List<ArticuloData>();
            for (int i = 0; i < itemPool.Count; i++)
            {
                if (itemPool[i] != null && !available.Contains(itemPool[i]))
                    available.Add(itemPool[i]);
            }

            var random = seed.HasValue
                ? new System.Random(seed.Value)
                : new System.Random(UnityEngine.Random.Range(0, int.MaxValue));
            int count = Mathf.Min(definition.choiceCount, available.Count);

            for (int i = 0; i < count; i++)
            {
                Rareza rolled = RollRarity(definition, random);
                ArticuloData selected = TakeByClosestRarity(available, rolled, random);
                if (selected == null)
                    break;

                contents.Add(selected);
                available.Remove(selected);
            }

            return new ItemPackOffer(definition, contents);
        }

        static Rareza RollRarity(ItemPackData definition, System.Random random)
        {
            float total = 0f;
            for (int i = 0; i < definition.rarityWeights.Count; i++)
                total += Mathf.Max(0f, definition.rarityWeights[i].weight);

            if (total <= 0f)
                return definition.displayRarity;

            double roll = random.NextDouble() * total;
            float cumulative = 0f;
            for (int i = 0; i < definition.rarityWeights.Count; i++)
            {
                cumulative += Mathf.Max(0f, definition.rarityWeights[i].weight);
                if (roll < cumulative)
                    return definition.rarityWeights[i].rarity;
            }

            return definition.displayRarity;
        }

        static ArticuloData TakeByClosestRarity(List<ArticuloData> available, Rareza target, System.Random random)
        {
            for (int distance = 0; distance <= 2; distance++)
            {
                var candidates = new List<ArticuloData>();
                for (int i = 0; i < available.Count; i++)
                {
                    if (Math.Abs((int)available[i].rareza - (int)target) == distance)
                        candidates.Add(available[i]);
                }

                if (candidates.Count > 0)
                    return candidates[random.Next(0, candidates.Count)];
            }

            return null;
        }
    }
}
