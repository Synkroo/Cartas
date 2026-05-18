namespace JuegoDeCartas.Stats
{
    [System.Serializable]
    public class StatModifier
    {
        public enum Target
        {
            Player,
            Enemy,
            Both
        }

        public enum Operation
        {
            Add,
            Remove
        }

        public Target target;
        public StatType stat;
        public Operation operation;
        public int amount;
    }
}
