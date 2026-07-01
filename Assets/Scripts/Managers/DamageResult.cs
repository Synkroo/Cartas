namespace JuegoDeCartas.Managers
{
    public readonly struct DamageResult
    {
        public readonly int attemptedDamage;
        public readonly int armorAbsorbed;
        public readonly int healthDamage;
        public readonly bool defeated;
        public readonly bool revived;
        public readonly bool ignored;

        public int AppliedDamage => armorAbsorbed + healthDamage;

        public DamageResult(
            int attemptedDamage,
            int armorAbsorbed,
            int healthDamage,
            bool defeated,
            bool revived = false,
            bool ignored = false)
        {
            this.attemptedDamage = attemptedDamage;
            this.armorAbsorbed = armorAbsorbed;
            this.healthDamage = healthDamage;
            this.defeated = defeated;
            this.revived = revived;
            this.ignored = ignored;
        }

        public DamageResult WithRevive()
        {
            return new DamageResult(
                attemptedDamage,
                armorAbsorbed,
                healthDamage,
                false,
                true,
                ignored
            );
        }

        public static DamageResult Ignored(int attemptedDamage)
        {
            return new DamageResult(
                attemptedDamage,
                0,
                0,
                false,
                false,
                true
            );
        }
    }
}
