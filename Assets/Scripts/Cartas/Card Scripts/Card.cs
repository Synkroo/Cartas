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

        public int effectiveCost => data != null ? System.Math.Max(0, data.cost - costReduction) : 0;
        public bool effectiveDestroyOnUse => data != null && data.destroyOnUse && !preventDestroyOnUse;
        public CardUpgradeOption SelectedUpgrade =>
            data != null &&
            selectedUpgradeIndex >= 0 &&
            selectedUpgradeIndex < data.upgradeOptions.Count
                ? data.upgradeOptions[selectedUpgradeIndex]
                : null;

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
    }
}
