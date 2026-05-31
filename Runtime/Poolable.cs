using System.Threading;
using UnityEngine;

namespace Demondragon.PrefabPooling
{
    /// <summary>
    /// Convenient base class for all poolable <see cref="MonoBehaviour"/>
    /// components. Inherit from this instead of implementing
    /// <see cref="IPoolable{T}"/> directly.
    /// </summary>
    /// <typeparam name="T">
    /// The concrete subclass (CRTP), e.g.
    /// <c>class Bullet : Poolable&lt;Bullet&gt;</c>.
    /// </typeparam>
    /// <remarks>
    /// <b>Lifecycle:</b>
    /// <list type="number">
    ///   <item><description><see cref="SetPoolData"/> — called once after instantiation.</description></item>
    ///   <item><description><see cref="OnSpawned"/> — called on every <see cref="IPoolManager.Get{T}"/>. Creates a fresh <see cref="DespawnToken"/> and activates the GameObject.</description></item>
    ///   <item><description><see cref="OnDespawned"/> — called on every <see cref="Release"/>. Cancels the <see cref="DespawnToken"/>, stops coroutines, deactivates the GameObject.</description></item>
    /// </list>
    /// <para>
    /// <b>Async (Awaitable):</b> Always pass <see cref="DespawnToken"/> to every
    /// <c>Awaitable</c> call in a subclass so that async methods are properly
    /// cancelled when the object is returned to the pool.
    /// </para>
    /// <example>
    /// <code>
    /// public class Bullet : Poolable&lt;Bullet&gt;
    /// {
    ///     public override async void OnSpawned()
    ///     {
    ///         base.OnSpawned();
    ///         await MoveAsync(DespawnToken);
    ///     }
    ///
    ///     private async Awaitable MoveAsync(CancellationToken ct)
    ///     {
    ///         while (!ct.IsCancellationRequested)
    ///         {
    ///             transform.position += Vector3.forward * Time.deltaTime * 10f;
    ///             await Awaitable.NextFrameAsync(ct);
    ///         }
    ///     }
    /// }
    /// </code>
    /// </example>
    /// </remarks>
    public abstract class Poolable<T> : MonoBehaviour, IPoolable<T> where T : MonoBehaviour, IPoolable<T>
    {
        public IPoolManager PoolManager { get; private set; }
        public GameObject PoolPrefab { get; private set; }

        private CancellationTokenSource _despawnCts;

        /// <summary>
        /// Cancelled every time this object is returned to the pool.
        /// Pass this token to all Awaitable async methods in subclasses to ensure
        /// they are properly cancelled on despawn.
        /// </summary>
        protected CancellationToken DespawnToken => _despawnCts?.Token ?? CancellationToken.None;

        public virtual void SetPoolData(IPoolManager manager, GameObject prefab)
        {
            PoolManager = manager;
            PoolPrefab = prefab;
        }

        public virtual void OnSpawned()
        {
            _despawnCts = new CancellationTokenSource();
            gameObject.SetActive(true);
        }

        public virtual void OnDespawned()
        {
            if (_despawnCts != null)
            {
                _despawnCts.Cancel();
                _despawnCts.Dispose();
                _despawnCts = null;
            }

            CancelInvoke();
            StopAllCoroutines();

            gameObject.SetActive(false);
        }

        /// <summary>Returns this object to its pool.</summary>
        public void Release()
        {
            if (PoolManager != null && PoolPrefab != null)
            {
                PoolManager.Release(PoolPrefab, this as T);
            }
            else
            {
                Debug.LogWarning($"[Poolable] {gameObject.name} has no PoolManager reference. Destroying instead.");
                if (Application.isPlaying)
                    Destroy(gameObject);
                else
                    DestroyImmediate(gameObject);
            }
        }
    }
}
