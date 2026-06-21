using System;

namespace JuegoDeCartas.Challenges
{
    public static class RunRandom
    {
        static Random random;

        public static int CurrentSeed { get; private set; }
        public static bool IsInitialized => random != null;

        public static void Initialize(string seedCode)
        {
            Initialize(StableHash(NormalizeSeedCode(seedCode)));
        }

        public static void Initialize(int seed)
        {
            CurrentSeed = seed;
            random = new Random(seed);
        }

        public static void InitializeUnseeded()
        {
            Initialize(unchecked(Environment.TickCount * 397 ^ Guid.NewGuid().GetHashCode()));
        }

        public static int Range(int minInclusive, int maxExclusive)
        {
            EnsureInitialized();
            if (maxExclusive <= minInclusive)
                return minInclusive;
            return random.Next(minInclusive, maxExclusive);
        }

        public static double NextDouble()
        {
            EnsureInitialized();
            return random.NextDouble();
        }

        public static int NextSeed()
        {
            EnsureInitialized();
            return unchecked(random.Next() ^ (random.Next() << 1));
        }

        public static string GenerateSeedCode()
        {
            string raw = Guid.NewGuid().ToString("N").ToUpperInvariant();
            return raw.Substring(0, 8);
        }

        public static string NormalizeSeedCode(string seedCode)
        {
            return string.IsNullOrWhiteSpace(seedCode)
                ? GenerateSeedCode()
                : seedCode.Trim().ToUpperInvariant();
        }

        public static int StableHash(string value)
        {
            unchecked
            {
                const uint offset = 2166136261;
                const uint prime = 16777619;
                uint hash = offset;
                string text = value ?? "";
                for (int i = 0; i < text.Length; i++)
                {
                    hash ^= text[i];
                    hash *= prime;
                }
                return (int)hash;
            }
        }

        static void EnsureInitialized()
        {
            if (random == null)
                InitializeUnseeded();
        }
    }
}
