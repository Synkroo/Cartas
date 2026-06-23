using System.Collections.Generic;
using JuegoDeCartas.Effects;
using JuegoDeCartas.Stats;

namespace JuegoDeCartas.Cards
{
    public static class CardDescriptionBuilder
    {
        public static string BuildFinalDescription(Card card)
        {
            return BuildFinalDescription(card, null);
        }

        public static string BuildFinalDescription(
            Card card,
            CardEpiphany previewEpiphany)
        {
            if (card?.data == null)
                return "";

            CardEpiphany epiphany = previewEpiphany ?? card.Epiphany;
            int previewReactivations = previewEpiphany != null &&
                                       card.Epiphany == null
                ? previewEpiphany.reactivations
                : 0;
            int repeats = 1 + card.reactivationCount + previewReactivations;
            EffectSummary summary = new EffectSummary();

            AddEffects(summary, card.data.effects, repeats);

            CardUpgradeOption upgrade = card.SelectedUpgrade;
            if (upgrade != null)
                AddEffects(summary, upgrade.bonusEffects, 1);

            if (epiphany != null)
                AddEffects(summary, epiphany.bonusEffects, 1);

            List<string> parts = BuildParts(summary);
            bool preventsDestroy = card.preventDestroyOnUse ||
                                   (previewEpiphany != null &&
                                    previewEpiphany.preventDestroyOnUse);
            if (card.data.destroyOnUse)
            {
                parts.Add(preventsDestroy
                    ? "No se destruye al usarla."
                    : "Se destruye al usarla.");
            }

            if (parts.Count > 0)
                return string.Join(" ", parts);

            if (epiphany != null)
                return epiphany.GetCardDescription();

            return card.data.description ?? "";
        }

        static void AddEffects(
            EffectSummary summary,
            IEnumerable<CardEffect> effects,
            int multiplier)
        {
            if (effects == null)
                return;

            foreach (CardEffect effect in effects)
            {
                if (effect == null)
                    continue;

                int appliedMultiplier = System.Math.Max(1, multiplier);
                if (effect is ModifyStatsEffect statsEffect)
                {
                    AddStatModifiers(
                        summary,
                        statsEffect.modifiers,
                        appliedMultiplier
                    );
                }
                else if (effect is DrawEffect drawEffect)
                {
                    summary.cardsDrawn += drawEffect.amount * appliedMultiplier;
                }
                else if (effect is PlayerDamageBuffEffect buffEffect)
                {
                    summary.damageBonus += buffEffect.amount * appliedMultiplier;
                    summary.damageBonusTurns = System.Math.Max(
                        summary.damageBonusTurns,
                        buffEffect.turns
                    );
                }
                else if (effect is EpiphanyEffect epiphanyEffect)
                {
                    summary.damage += epiphanyEffect.damage * appliedMultiplier;
                    summary.armor += epiphanyEffect.armor * appliedMultiplier;
                    summary.cardsDrawn +=
                        epiphanyEffect.cardsToDraw * appliedMultiplier;
                    summary.manaRestored +=
                        epiphanyEffect.manaToRestore * appliedMultiplier;
                    summary.healthRestored +=
                        epiphanyEffect.healthToRestore * appliedMultiplier;
                    summary.goldGained +=
                        epiphanyEffect.goldToGain * appliedMultiplier;
                    summary.damageBonus +=
                        epiphanyEffect.damageBonus * appliedMultiplier;
                    summary.damageBonusTurns = System.Math.Max(
                        summary.damageBonusTurns,
                        epiphanyEffect.damageBonusTurns
                    );
                }
            }
        }

        static void AddStatModifiers(
            EffectSummary summary,
            IEnumerable<StatModifier> modifiers,
            int multiplier)
        {
            if (modifiers == null)
                return;

            foreach (StatModifier modifier in modifiers)
            {
                if (modifier == null)
                    continue;

                int amount = modifier.amount * multiplier;
                bool affectsPlayer =
                    modifier.target == StatModifier.Target.Player ||
                    modifier.target == StatModifier.Target.Both;
                bool affectsEnemy =
                    modifier.target == StatModifier.Target.Enemy ||
                    modifier.target == StatModifier.Target.Both;

                if (affectsEnemy &&
                    modifier.stat == StatType.Health &&
                    modifier.operation == StatModifier.Operation.Remove)
                {
                    summary.damage += amount;
                }

                if (!affectsPlayer ||
                    modifier.operation != StatModifier.Operation.Add)
                {
                    continue;
                }

                switch (modifier.stat)
                {
                    case StatType.Armor:
                        summary.armor += amount;
                        break;
                    case StatType.Health:
                        summary.healthRestored += amount;
                        break;
                    case StatType.Mana:
                        summary.manaRestored += amount;
                        break;
                }
            }
        }

        static List<string> BuildParts(EffectSummary summary)
        {
            List<string> parts = new List<string>();

            if (summary.damage > 0)
                parts.Add($"Inflige {summary.damage} de daño.");
            if (summary.armor > 0)
                parts.Add($"Obtiene {summary.armor} de armadura.");
            if (summary.cardsDrawn > 0)
            {
                parts.Add(summary.cardsDrawn == 1
                    ? "Roba 1 carta."
                    : $"Roba {summary.cardsDrawn} cartas.");
            }
            if (summary.manaRestored > 0)
                parts.Add($"Recupera {summary.manaRestored} de maná.");
            if (summary.healthRestored > 0)
                parts.Add($"Recupera {summary.healthRestored} de vida.");
            if (summary.goldGained > 0)
                parts.Add($"Obtiene {summary.goldGained} de oro.");
            if (summary.damageBonus > 0)
            {
                parts.Add(
                    $"Obtiene +{summary.damageBonus} de daño durante " +
                    $"{summary.damageBonusTurns} turnos."
                );
            }

            return parts;
        }

        sealed class EffectSummary
        {
            public int damage;
            public int armor;
            public int cardsDrawn;
            public int manaRestored;
            public int healthRestored;
            public int goldGained;
            public int damageBonus;
            public int damageBonusTurns;
        }
    }
}
