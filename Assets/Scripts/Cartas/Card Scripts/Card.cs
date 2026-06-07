namespace JuegoDeCartas.Cards
{
    public class Card
    {
        public CardData data;
        public bool upgraded;
        public int costReduction;
        public int reactivationCount;

        public int effectiveCost => data != null ? System.Math.Max(0, data.cost - costReduction) : 0;

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
    }
}
