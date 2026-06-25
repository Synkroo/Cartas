using System.Collections.Generic;
using JuegoDeCartas.Effects;
using JuegoDeCartas.Enemies;
using JuegoDeCartas.Managers;
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

        public static string BuildInspectionDescription(
            Card card,
            BattleManager battle = null)
        {
            if (card?.data == null)
                return "";

            List<string> sections = new List<string>();
            string description = card.Epiphany != null
                ? BuildFinalDescription(card)
                : battle != null
                    ? battle.GetRuntimeCardDescription(card.data)
                    : card.data.description;

            if (!string.IsNullOrWhiteSpace(description))
                sections.Add(description);

            sections.Add("Coste actual: " + card.effectiveCost + ".");

            string upgradeText = BuildRuntimeUpgradeText(card);
            if (!string.IsNullOrWhiteSpace(upgradeText))
                sections.Add("Estado:\n" + upgradeText);

            List<string> effectDetails = BuildEffectDetails(card);
            if (effectDetails.Count > 0)
                sections.Add("Efectos:\n- " + string.Join("\n- ", effectDetails));

            List<string> mechanicNotes = BuildMechanicNotes(card);
            if (mechanicNotes.Count > 0)
                sections.Add("Mecanicas:\n- " + string.Join("\n- ", mechanicNotes));

            return string.Join("\n\n", sections);
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

        static string BuildRuntimeUpgradeText(Card card)
        {
            if (card == null)
                return "";

            List<string> parts = new List<string>();
            if (card.costReduction > 0)
                parts.Add("Coste reducido en " + card.costReduction + ".");
            if (card.reactivationCount > 0)
                parts.Add("Se usa " + (card.reactivationCount + 1) + " veces.");
            if (card.preventDestroyOnUse)
                parts.Add("No se destruye al usarla.");
            if (card.frozen)
                parts.Add("Esta congelada.");
            if (card.burned)
                parts.Add("Fue quemada al menos una vez.");

            return string.Join(" ", parts);
        }

        static List<string> BuildEffectDetails(Card card)
        {
            List<string> details = new List<string>();
            if (card?.data == null)
                return details;

            int repeats = 1 + card.reactivationCount;
            AddEffectDetails(details, card.data.effects, repeats);

            CardUpgradeOption upgrade = card.SelectedUpgrade;
            if (upgrade != null)
                AddEffectDetails(details, upgrade.bonusEffects, 1);

            CardEpiphany epiphany = card.Epiphany;
            if (epiphany != null)
                AddEffectDetails(details, epiphany.bonusEffects, 1);

            if (card.data.destroyOnUse && !card.preventDestroyOnUse)
                details.Add("Se destruye despues de usarse.");
            else if (card.data.destroyOnUse && card.preventDestroyOnUse)
                details.Add("Normalmente se destruiria, pero esta copia lo evita.");

            return details;
        }

        static void AddEffectDetails(
            List<string> details,
            IEnumerable<CardEffect> effects,
            int multiplier)
        {
            if (effects == null)
                return;

            int repeats = System.Math.Max(1, multiplier);
            foreach (CardEffect effect in effects)
            {
                if (effect == null)
                    continue;

                if (effect is CombatMechanicEffect mechanic)
                {
                    AddUnique(details, DescribeCombatMechanic(mechanic, repeats));
                }
                else if (effect is ModifyStatsEffect statsEffect)
                {
                    AddStatModifierDetails(details, statsEffect.modifiers, repeats);
                }
                else if (effect is DrawEffect drawEffect)
                {
                    AddUnique(details, "Roba " + (drawEffect.amount * repeats) + " carta(s).");
                }
                else if (effect is PlayerDamageBuffEffect buffEffect)
                {
                    AddUnique(
                        details,
                        "Ganas +" + (buffEffect.amount * repeats) +
                        " de daño durante " + buffEffect.turns + " turno(s)."
                    );
                }
                else if (effect is EpiphanyEffect epiphanyEffect)
                {
                    AddEpiphanyEffectDetails(details, epiphanyEffect, repeats);
                }
            }
        }

        static void AddStatModifierDetails(
            List<string> details,
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
                string target = modifier.target switch
                {
                    StatModifier.Target.Enemy => "enemigo",
                    StatModifier.Target.Both => "ambos",
                    _ => "jugador"
                };
                string operation = modifier.operation == StatModifier.Operation.Add
                    ? "+"
                    : "-";
                AddUnique(
                    details,
                    target + ": " + operation + amount + " " +
                    GetStatLabel(modifier.stat) + "."
                );
            }
        }

        static void AddEpiphanyEffectDetails(
            List<string> details,
            EpiphanyEffect effect,
            int multiplier)
        {
            if (effect.damage > 0)
                AddUnique(details, "Inflige " + (effect.damage * multiplier) + " daño.");
            if (effect.armor > 0)
                AddUnique(details, "Ganas " + (effect.armor * multiplier) + " armadura.");
            if (effect.cardsToDraw > 0)
                AddUnique(details, "Roba " + (effect.cardsToDraw * multiplier) + " carta(s).");
            if (effect.manaToRestore > 0)
                AddUnique(details, "Recupera " + (effect.manaToRestore * multiplier) + " mana.");
            if (effect.healthToRestore > 0)
                AddUnique(details, "Cura " + (effect.healthToRestore * multiplier) + " vida.");
            if (effect.goldToGain > 0)
                AddUnique(details, "Gana " + (effect.goldToGain * multiplier) + " oro.");
            if (effect.damageBonus > 0)
            {
                AddUnique(
                    details,
                    "Ganas +" + (effect.damageBonus * multiplier) +
                    " de daño durante " + effect.damageBonusTurns + " turno(s)."
                );
            }
        }

        static string DescribeCombatMechanic(
            CombatMechanicEffect mechanic,
            int multiplier)
        {
            int amount = mechanic.amount * System.Math.Max(1, multiplier);
            switch (mechanic.action)
            {
                case CombatMechanicAction.DealDamage:
                    return "Inflige " + amount + " daño.";
                case CombatMechanicAction.GainArmor:
                    return "Ganas " + amount + " armadura.";
                case CombatMechanicAction.RestoreMana:
                    return "Recupera " + amount + " mana.";
                case CombatMechanicAction.DrawCards:
                    return "Roba " + amount + " carta(s).";
                case CombatMechanicAction.FreezeCards:
                    return "Elige y congela " + amount + " carta(s) de la mano.";
                case CombatMechanicAction.ShatterFrozenCards:
                    return "Rompe " + GetCardAmountLabel(mechanic.amount) +
                           " congelada(s): coste -" + mechanic.costReduction +
                           ", +" + mechanic.reactivations + " reactivacion(es) y " +
                           mechanic.secondaryAmount + " daño por carta.";
                case CombatMechanicAction.BurnCards:
                    return "Quema " + amount + " carta(s). " +
                           (mechanic.secondaryAmount > 0
                               ? "Cada una anade " + mechanic.secondaryAmount + " daño."
                               : "Cuenta como descarte.");
                case CombatMechanicAction.DamagePerFrozenCard:
                    return "Inflige " + mechanic.amount + " + " +
                           mechanic.secondaryAmount + " daño por carta congelada.";
                case CombatMechanicAction.DamageByMissingHandCards:
                    return "Inflige " + mechanic.amount + " + " +
                           mechanic.secondaryAmount +
                           " daño por hueco de mano hasta " + mechanic.threshold + ".";
                case CombatMechanicAction.DamageByBurnedCardsThisTurn:
                    return "Inflige " + mechanic.amount + " + " +
                           mechanic.secondaryAmount +
                           " daño por carta quemada este turno.";
                case CombatMechanicAction.DamagePerEnemyStatusStack:
                    return "Inflige " + mechanic.amount + " + " +
                           mechanic.secondaryAmount + " daño por acumulacion de " +
                           GetStatusLabel(mechanic.statusType) + ".";
                case CombatMechanicAction.ApplyEnemyStatus:
                    return "Aplica " + amount + " " +
                           GetStatusLabel(mechanic.statusType) + ".";
                case CombatMechanicAction.CriticalDamageFromWeakness:
                    return "Inflige " + amount +
                           " daño y consume Debilidad para ganar +" +
                           mechanic.percentage + "% por acumulacion.";
                case CombatMechanicAction.DetonateBleed:
                    return "Consume " + mechanic.percentage +
                           "% del Sangrado y causa ese daño inmediatamente.";
                case CombatMechanicAction.SpendManaForDamage:
                    return "Gasta hasta " + mechanic.amount + " mana para infligir " +
                           mechanic.secondaryAmount + " daño por mana.";
                case CombatMechanicAction.SpendHealthForDamage:
                    return "Pierdes " + mechanic.amount + " vida para infligir " +
                           mechanic.secondaryAmount + " daño por vida perdida.";
            }

            return "";
        }

        static string GetCardAmountLabel(int amount)
        {
            return amount <= 0 ? "todas las cartas" : amount.ToString();
        }

        static string GetStatusLabel(EnemyStatusType statusType)
        {
            return statusType switch
            {
                EnemyStatusType.Weakness => "Debilidad",
                EnemyStatusType.Poison => "Veneno",
                EnemyStatusType.Bleed => "Sangrado",
                EnemyStatusType.Stun => "Stun",
                _ => statusType.ToString()
            };
        }

        static string GetStatLabel(StatType stat)
        {
            return stat switch
            {
                StatType.Health => "vida",
                StatType.MaxHealth => "vida maxima",
                StatType.Mana => "mana",
                StatType.MaxMana => "mana maximo",
                StatType.Damage => "daño",
                StatType.Armor => "armadura",
                _ => stat.ToString()
            };
        }

        static void AddUnique(List<string> details, string detail)
        {
            if (!string.IsNullOrWhiteSpace(detail) &&
                !details.Contains(detail))
            {
                details.Add(detail);
            }
        }

        static List<string> BuildMechanicNotes(Card card)
        {
            List<string> notes = new List<string>();
            if (card?.data?.effects == null)
                return notes;

            AddMechanicNotes(notes, card.data.effects);

            CardUpgradeOption upgrade = card.SelectedUpgrade;
            if (upgrade != null)
                AddMechanicNotes(notes, upgrade.bonusEffects);

            CardEpiphany epiphany = card.Epiphany;
            if (epiphany != null)
                AddMechanicNotes(notes, epiphany.bonusEffects);

            return notes;
        }

        static void AddMechanicNotes(
            List<string> notes,
            IEnumerable<CardEffect> effects)
        {
            if (effects == null)
                return;

            foreach (CardEffect effect in effects)
            {
                CombatMechanicEffect mechanic = effect as CombatMechanicEffect;
                if (mechanic == null)
                    continue;

                string note = GetMechanicNote(mechanic);
                if (!string.IsNullOrWhiteSpace(note) &&
                    !notes.Contains(note))
                {
                    notes.Add(note);
                }
            }
        }

        static string GetMechanicNote(CombatMechanicEffect mechanic)
        {
            switch (mechanic.action)
            {
                case CombatMechanicAction.FreezeCards:
                    return "Congelar: eliges una carta de tu mano; se conserva este turno y dos turnos de jugador mas, o hasta romperla.";
                case CombatMechanicAction.ShatterFrozenCards:
                    return "Romper: eliges una carta congelada, la descongelas y aplicas la mejora indicada.";
                case CombatMechanicAction.BurnCards:
                    return "Quemar: eliges una carta de tu mano; cuenta como descartar.";
                case CombatMechanicAction.ApplyEnemyStatus:
                    return GetStatusNote(mechanic.statusType);
                case CombatMechanicAction.CriticalDamageFromWeakness:
                    return "Debilidad consumida: +2% de daño por acumulacion consumida.";
                case CombatMechanicAction.DetonateBleed:
                    return "Sangrado detonado: consume sangrado para causar daño inmediato.";
            }

            return "";
        }

        static string GetStatusNote(EnemyStatusType statusType)
        {
            switch (statusType)
            {
                case EnemyStatusType.Weakness:
                    return "Debilidad: cada acumulacion da +2% de daño al consumirla; baja 1 si no aplicas mas.";
                case EnemyStatusType.Poison:
                    return "Veneno: no se acumula; causa 5% de la vida maxima del enemigo al empezar su turno.";
                case EnemyStatusType.Bleed:
                    return "Sangrado: causa 1 daño por acumulacion; desaparece si no aplicas sangrado en tu siguiente turno.";
                case EnemyStatusType.Stun:
                    return "Stun: al llegar a " + BattleManager.StunThreshold + " cancela el proximo ataque enemigo.";
            }

            return "";
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
