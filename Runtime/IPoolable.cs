using UnityEngine;

namespace Demondragon.PrefabPooling
{
    /// <summary>
    /// Marks a <see cref="MonoBehaviour"/> as compatible with
    /// <see cref="IPoolManager"/>.
    /// </summary>
    /// <typeparam name="T">
    /// The concrete component type (CRTP self-reference), e.g.
    /// <c>class Bullet : Poolable&lt;Bullet&gt;</c>.
    /// </typeparam>
    /// <remarks>
    /// Prefer inheriting from <see cref="Poolable{T}"/> instead of implementing
    /// this interface directly. Direct implementation is only necessary when the
    /// component already inherits from another <c>MonoBehaviour</c> subclass.
    /// </remarks>
    public interface IPoolable<T> where T : MonoBehaviour, IPoolable<T>
    {
        /// <summary>
        /// The <see cref="IPoolManager"/> that owns this instance.
        /// Set internally by <see cref="SetPoolData"/> — do not assign externally.
        /// </summary>
        IPoolManager PoolManager { get; }

        /// <summary>
        /// The prefab this instance was created from.
        /// Used by <see cref="Release"/> to return the object to the correct pool.
        /// Set internally by <see cref="SetPoolData"/> — do not assign externally.
        /// </summary>
        GameObject PoolPrefab { get; }

        /// <summary>
        /// Called once after the instance is instantiated to inject pool
        /// ownership data. Invoked by <see cref="IPoolManager"/> — do not call
        /// manually.
        /// </summary>
        void SetPoolData(IPoolManager manager, GameObject prefab);

        /// <summary>
        /// Called by the pool every time this instance is retrieved via
        /// <see cref="IPoolManager.Get{T}"/>. Use this to reset state and
        /// activate the object instead of <c>Awake</c> / <c>Start</c>.
        /// </summary>
        void OnSpawned();

        /// <summary>
        /// Called by the pool every time this instance is returned via
        /// <see cref="IPoolManager.Release{T}"/> or <see cref="Release"/>.
        /// Use this to clean up state (cancel async operations, stop coroutines,
        /// hide the object) instead of <c>OnDestroy</c>.
        /// </summary>
        void OnDespawned();

        /// <summary>
        /// Convenience method that returns this instance to its pool by calling
        /// <see cref="IPoolManager.Release{T}"/>. Falls back to
        /// <c>Object.Destroy</c> when <see cref="PoolManager"/> is not set.
        /// </summary>
        void Release();
    }
}
