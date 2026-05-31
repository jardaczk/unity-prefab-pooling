using UnityEngine;

namespace Demondragon.PrefabPooling
{
    /// <summary>
    /// Central contract for the prefab-based object pool system.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Each unique <see cref="GameObject"/> prefab gets its own typed
    /// <c>ObjectPool&lt;T&gt;</c> managed internally. The first call to
    /// <see cref="Get{T}"/> for a given prefab creates the pool using
    /// <paramref name="config"/> (or <see cref="PoolConfig.Default"/> when the
    /// default value is passed). Subsequent calls reuse the same pool.
    /// </para>
    /// <para>
    /// All pooled objects are parented to an internal <c>[PoolManager]</c>
    /// root <c>GameObject</c> that survives scene loads via
    /// <c>DontDestroyOnLoad</c>.
    /// </para>
    /// <para>
    /// The concrete implementation <see cref="PoolManager"/> also implements
    /// <see cref="System.IDisposable"/>. Always dispose the manager when it is
    /// no longer needed (e.g. when unloading) to destroy pooled objects and
    /// free memory.
    /// </para>
    /// <example>
    /// Basic usage:
    /// <code>
    /// IPoolManager pool = new PoolManager();
    ///
    /// // Optionally pre-warm during a loading screen
    /// pool.Prewarm&lt;Bullet&gt;(bulletPrefab, 20);
    ///
    /// // Spawn
    /// Bullet b = pool.Get&lt;Bullet&gt;(bulletPrefab);
    ///
    /// // Return to pool (or call b.Release() directly)
    /// pool.Release(bulletPrefab, b);
    /// </code>
    /// </example>
    /// </remarks>
    public interface IPoolManager
    {
        /// <summary>
        /// Gets an instance from the pool, or creates a new one if the pool is
        /// empty. Calls <see cref="IPoolable{T}.OnSpawned"/> on the returned
        /// instance.
        /// </summary>
        T Get<T>(GameObject prefab, PoolConfig config = default) where T : MonoBehaviour, IPoolable<T>;

        /// <summary>
        /// Returns an instance to its pool. Calls
        /// <see cref="IPoolable{T}.OnDespawned"/> and re-parents the object
        /// under the pool root.
        /// </summary>
        void Release<T>(GameObject prefab, T instance) where T : MonoBehaviour, IPoolable<T>;

        /// <summary>
        /// Pre-warms the pool by creating <paramref name="count"/> instances
        /// upfront and immediately returning them to the pool in an inactive
        /// state.
        /// </summary>
        void Prewarm<T>(GameObject prefab, int count, PoolConfig config = default) where T : MonoBehaviour, IPoolable<T>;

        /// <summary>
        /// Destroys all pooled instances for the given prefab and removes the pool.
        /// </summary>
        void Clear(GameObject prefab);

        /// <summary>
        /// Destroys all pooled instances across every pool and removes them.
        /// </summary>
        void ClearAll();
    }
}
