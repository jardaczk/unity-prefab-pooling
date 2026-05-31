namespace Demondragon.PrefabPooling
{
    /// <summary>
    /// Immutable configuration for a single prefab pool.
    /// Pass an instance to <see cref="IPoolManager.Get{T}"/> or
    /// <see cref="IPoolManager.Prewarm{T}"/> to control the pool capacity.
    /// </summary>
    /// <remarks>
    /// When the default value (<c>default</c>) is passed to a pool method the
    /// manager falls back to <see cref="Default"/> automatically, so you only
    /// need to supply a custom config when the defaults are not appropriate.
    /// </remarks>
    public readonly struct PoolConfig
    {
        /// <summary>Initial internal capacity of the pool list.</summary>
        public readonly int DefaultCapacity;

        /// <summary>
        /// Maximum number of idle instances kept in the pool.
        /// If a released instance would exceed this limit it is destroyed
        /// immediately instead of being pooled.
        /// </summary>
        public readonly int MaxSize;

        /// <param name="defaultCapacity">Initial list capacity (must be &gt; 0).</param>
        /// <param name="maxSize">Maximum pooled instances (must be &gt;= <paramref name="defaultCapacity"/>).</param>
        public PoolConfig(int defaultCapacity, int maxSize)
        {
            DefaultCapacity = defaultCapacity;
            MaxSize = maxSize;
        }

        /// <summary>
        /// Sensible default: <see cref="DefaultCapacity"/> = 10, <see cref="MaxSize"/> = 100.
        /// </summary>
        public static PoolConfig Default => new PoolConfig(10, 100);
    }
}
