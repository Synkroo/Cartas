using System;
using System.Collections.Generic;
using UnityEngine;
using JuegoDeCartas.Challenges;

namespace JuegoDeCartas.Articulos
{
    public static class ItemPackGenerator
    {
        public static ItemPackData RollDefinition(List<ItemPackData> definitions, int? seed = null)
        {
            if (definitions == null)
                return null;

            var available = new List<ItemPackData>();
            float totalWeight = 0f;
            for (int i = 0; i < definitions.Count; i++)
            {
                ItemPackData definition = definitions[i];
                if (definition == null || definition.shopAppearanceWeight <= 0f)
                    continue;

                available.Add(definition);
                totalWeight += definition.shopAppearanceWeight;
            }

            if (available.Count == 0 || totalWeight <= 0f)
                return null;

            var random = seed.HasValue
                ? new System.Random(seed.Value)
                : new System.Random(RunRandom.NextSeed());
            double roll = random.NextDouble() * totalWeight;
            float cumulative = 0f;

            for (int i = 0; i < available.Count; i++)
            {
                cumulative += available[i].shopAppearanceWeight;
                if (roll < cumulative)
                    return available[i];
            }

            return available[available.Count - 1];
        }

        public static ItemPackOffer Generate(ItemPackData definition, List<ArticuloData> itemPool, int? seed = null)
        {
            var contents = new List<ArticuloData>();
            if (definition == null || itemPool == null)
                return new ItemPackOffer(definition, contents);

            var available = new List<ArticuloData>();
            for (int i = 0; i < itemPool.Count; i++)
            {
                if (definition.Allows(itemPool[i]) && !available.Contains(itemPool[i]))
                    available.Add(itemPool[i]);
            }

            var random = seed.HasValue
                ? new System.Random(seed.Value)
                : new System.Random(RunRandom.NextSeed());
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
