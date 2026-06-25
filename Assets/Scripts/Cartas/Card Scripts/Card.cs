using JuegoDeCartas.Progression;

namespace JuegoDeCartas.Cards
{
    public class Card
    {
        public CardData data;
        public bool upgraded;
        public int costReduction;
        public int reactivationCount;
        public int selectedUpgradeIndex = -1;
        public bool preventDestroyOnUse;
        public bool epiphanyUnlocked;
        public int selectedEpiphanyIndex = -1;
        public bool frozen;
        public int frozenTurnsRemaining;
        public bool burned;
        public int burnCount;

        public int effectiveCost => data != null
            ? System.Math.Max(0, data.cost - costReduction)
            : 0;
        public bool effectiveDestroyOnUse =>
            data != null && data.destroyOnUse && !preventDestroyOnUse;
        public CardUpgradeOption SelectedUpgrade =>
            data != null &&
            selectedUpgradeIndex >= 0 &&
            selectedUpgradeIndex < data.upgradeOptions.Count
                ? data.upgradeOptions[selectedUpgradeIndex]
                : null;
        public CardEpiphany Epiphany => ResolveEpiphany();

        public Card(CardData data)
        {
            this.data = data;
        }

        public Card(Card source, bool includeUpgrades)
        {
            data = source != null ? source.data : null;

            if (!includeUpgrades || source == null)
                return;

            upgraded = source.upgraded;
            costReduction = source.costReduction;
            reactivationCount = source.reactivationCount;
            selectedUpgradeIndex = source.selectedUpgradeIndex;
            preventDestroyOnUse = source.preventDestroyOnUse;
            epiphanyUnlocked = source.epiphanyUnlocked;
            selectedEpiphanyIndex = source.selectedEpiphanyIndex;
            frozen = false;
            frozenTurnsRemaining = 0;
            burned = source.burned;
            burnCount = source.burnCount;
        }

        public void ReduceCost(int amount = 1)
        {
            if (data == null || amount <= 0)
                return;

            costReduction = System.Math.Min(data.cost, costReduction + amount);
            upgraded = true;
        }

        public void AddReactivation(int amount = 1)
        {
            if (amount <= 0)
                return;

            reactivationCount += amount;
            upgraded = true;
        }

        public bool ApplyUpgrade(int optionIndex)
        {
            if (data == null ||
                selectedUpgradeIndex >= 0 ||
                optionIndex < 0 ||
                optionIndex >= data.upgradeOptions.Count)
            {
                return false;
            }

            CardUpgradeOption option = data.upgradeOptions[optionIndex];
            if (option == null)
                return false;

            selectedUpgradeIndex = optionIndex;
            ReduceCost(option.costReduction);
            AddReactivation(option.reactivations);
            preventDestroyOnUse = option.preventDestroyOnUse;
            upgraded = true;
            return true;
        }

        public bool ApplyEpiphany(int optionIndex = 0)
        {
            var options = data != null ? data.GetEpiphanyOptions() : null;
            if (data == null ||
                epiphanyUnlocked ||
                options == null ||
                optionIndex < 0 ||
                optionIndex >= options.Count ||
                options[optionIndex] == null ||
                string.IsNullOrWhiteSpace(options[optionIndex].epiphanyName))
            {
                return false;
            }

            CardEpiphany definition = options[optionIndex];
            epiphanyUnlocked = true;
            selectedEpiphanyIndex = optionIndex;
            ReduceCost(definition.costReduction);
            AddReactivation(definition.reactivations);
            preventDestroyOnUse |= definition.preventDestroyOnUse;
            upgraded = true;
            CollectionProgress.MarkEpiphanySeen(data, optionIndex);
            return true;
        }

        public void Freeze(int extraPlayerTurns)
        {
            frozen = true;
            frozenTurnsRemaining = System.Math.Max(0, extraPlayerTurns);
        }

        public void ClearFreeze()
        {
            frozen = false;
            frozenTurnsRemaining = 0;
        }

        public void MarkBurned()
        {
            burned = true;
            burnCount++;
        }

        public void ResetBurnCycle()
        {
            burnCount = 0;
        }

        CardEpiphany ResolveEpiphany()
        {
            if (!epiphanyUnlocked || data == null)
                return null;

            var options = data.GetEpiphanyOptions();
            int index = selectedEpiphanyIndex >= 0
                ? selectedEpiphanyIndex
                : 0;
            return index >= 0 && index < options.Count
                ? options[index]
                : null;
        }
    }
}
