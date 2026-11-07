namespace GMR.Core.Rng
{
    /// <summary>
    /// A seeded, reproducible random stream (AD-2). Gameplay code receives an IRng and never constructs one.
    /// </summary>
    public interface IRng
    {
        /// <summary>Next raw 32-bit value.</summary>
        uint NextUInt();

        /// <summary>Uniform float in [0, 1).</summary>
        float Value01();

        /// <summary>Uniform int in [minIncl, maxExcl). Throws if maxExcl &lt;= minIncl.</summary>
        int Range(int minIncl, int maxExcl);

        /// <summary>Uniform float in [min, max).</summary>
        float Range(float min, float max);

        /// <summary>
        /// Stateless child stream keyed by (name, index). The result depends only on this stream's
        /// seed, never on how many values were drawn or how many other children were derived.
        /// </summary>
        IRng Derive(string name, int index = 0);
    }
}
