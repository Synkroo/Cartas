namespace JuegoDeCartas.Cards
{
    public class Card
    {
        public CardData data;
        public bool upgraded;
        public int costReduction;
        public int reactivationCount;

        public int effectiveCost => (data.cost - costReduction);

        public Card(CardData data)
        {
            this.data = data;
        }
    }
}
