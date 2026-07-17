using System;

namespace CubeBlast.Core
{
    /// <summary>
    /// Abstraction over randomness so gameplay logic stays deterministic in tests.
    /// </summary>
    public interface IRandomProvider
    {
        /// <summary>Returns a value in [0, maxExclusive).</summary>
        int Next(int maxExclusive);
    }

    // Sealed to prevent inheritance
    public sealed class SystemRandomProvider : IRandomProvider
    {
        private readonly Random _random;

        public SystemRandomProvider() { _random = new Random(); }
        public SystemRandomProvider(int seed) { _random = new Random(seed); }

        public int Next(int maxExclusive) { return _random.Next(maxExclusive); }
    }
}
