using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace Demondragon.PrefabPooling
{
    /// <summary>
    /// Default implementation of <see cref="IPoolManager"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Internally uses <c>UnityEngine.Pool.ObjectPool&lt;T&gt;</c> — one pool
    /// per unique prefab reference. All idle instances are parented under a
    /// shared <c>[PoolManager]</c> root <c>GameObject</c> that survives scene
    /// loads via <c>DontDestroyOnLoad</c>.
    /// </para>
    /// <para>
    /// Thread safety: this class is <b>not</b> thread-safe. All calls must be
    /// made from the Unity main thread.
    /// </para>
    /// <para>
    /// Lifetime: call <see cref="Dispose"/> when the manager is no longer needed.
    /// Dispose destroys the root GameObject and all pooled instances. After
    /// disposal every <see cref="Get{T}"/> call returns <c>null</c> with an error log.
    /// </para>
    /// </remarks>
    public class PoolManager : IPoolManager, IDisposable
    {
        private sealed class PoolEntry : IDisposable
        {
            public readonly Type PooledType;
            public readonly IDisposable Pool;
            public readonly object TypedPool;

            public PoolEntry(Type pooledType, IDisposable pool, object typedPool)
            {
                PooledType = pooledType;
                Pool = pool;
                TypedPool = typedPool;
            }

            public void Dispose() => Pool.Dispose();
        }

        private readonly Dictionary<GameObject, PoolEntry> _pools = new();
        private Transform _poolRoot;
        private bool _disposed;

        public PoolManager()
        {
            var rootGo = new GameObject("[PoolManager]");
            _poolRoot = rootGo.transform;
            if (Application.isPlaying)
                UnityEngine.Object.DontDestroyOnLoad(rootGo);
        }

        public T Get<T>(GameObject prefab, PoolConfig config = default) where T : MonoBehaviour, IPoolable<T>
        {
            if (_disposed)
            {
                Debug.LogError("[PoolManager] Cannot get from disposed PoolManager");
                return null;
            }

            if (prefab == null)
            {
                Debug.LogError("[PoolManager] Prefab is null");
                return null;
            }

            if (_pools.TryGetValue(prefab, out var entry))
            {
                if (entry.PooledType != typeof(T))
                {
                    Debug.LogError($"[PoolManager] Type mismatch for prefab '{prefab.name}': pool holds {entry.PooledType.Name}, requested {typeof(T).Name}");
                    return null;
                }

                return ((ObjectPool<T>)entry.TypedPool).Get();
            }

            var resolvedConfig = config.MaxSize == 0 ? PoolConfig.Default : config;
            var newPool = CreatePool<T>(prefab, resolvedConfig);
            _pools[prefab] = new PoolEntry(typeof(T), newPool, newPool);
            return newPool.Get();
        }

        public void Prewarm<T>(GameObject prefab, int count, PoolConfig config = default) where T : MonoBehaviour, IPoolable<T>
        {
            if (_disposed || prefab == null || count <= 0) return;

            var resolvedConfig = config.MaxSize == 0 ? PoolConfig.Default : config;

            if (!_pools.TryGetValue(prefab, out var entry))
            {
                var newPool = CreatePool<T>(prefab, resolvedConfig);
                entry = new PoolEntry(typeof(T), newPool, newPool);
                _pools[prefab] = entry;
            }
            else if (entry.PooledType != typeof(T))
            {
                Debug.LogError($"[PoolManager] Type mismatch during Prewarm for prefab '{prefab.name}'");
                return;
            }

            var typedPool = (ObjectPool<T>)entry.TypedPool;
            var buffer = new T[count];
            for (int i = 0; i < count; i++)
                buffer[i] = typedPool.Get();
            for (int i = 0; i < count; i++)
                typedPool.Release(buffer[i]);
        }

        private ObjectPool<T> CreatePool<T>(GameObject prefab, PoolConfig config) where T : MonoBehaviour, IPoolable<T>
        {
            return new ObjectPool<T>(
                createFunc: () => CreateInstance<T>(prefab),
                actionOnGet: instance => instance.OnSpawned(),
                actionOnRelease: instance =>
                {
                    instance.transform.SetParent(_poolRoot);
                    instance.OnDespawned();
                },
                actionOnDestroy: instance =>
                {
                    if (instance != null && instance.gameObject != null)
                    {
                        if (Application.isPlaying)
                            UnityEngine.Object.Destroy(instance.gameObject);
                        else
                            UnityEngine.Object.DestroyImmediate(instance.gameObject);
                    }
                },
                collectionCheck: false,
                defaultCapacity: config.DefaultCapacity,
                maxSize: config.MaxSize
            );
        }

        private T CreateInstance<T>(GameObject prefab) where T : MonoBehaviour, IPoolable<T>
        {
            var go = UnityEngine.Object.Instantiate(prefab, _poolRoot);

            if (!go.TryGetComponent<T>(out var instance))
            {
                instance = go.AddComponent<T>();
                Debug.LogWarning($"[PoolManager] Prefab '{prefab.name}' does not have component {typeof(T).Name}. Adding it dynamically.");
            }

            instance.SetPoolData(this, prefab);
            return instance;
        }

        public void Release<T>(GameObject prefab, T instance) where T : MonoBehaviour, IPoolable<T>
        {
            if (_disposed)
            {
                if (instance != null && instance.gameObject != null)
                    UnityEngine.Object.Destroy(instance.gameObject);
                return;
            }

            if (instance == null) return;

            if (prefab != null && _pools.TryGetValue(prefab, out var entry))
            {
                ((ObjectPool<T>)entry.TypedPool).Release(instance);
            }
            else
            {
                if (instance.gameObject != null)
                    UnityEngine.Object.Destroy(instance.gameObject);
            }
        }

        public void Clear(GameObject prefab)
        {
            if (prefab == null) return;

            if (_pools.TryGetValue(prefab, out var entry))
            {
                entry.Dispose();
                _pools.Remove(prefab);
            }
        }

        public void ClearAll()
        {
            foreach (var entry in _pools.Values)
                entry.Dispose();
            _pools.Clear();
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            ClearAll();

            if (_poolRoot != null)
            {
                if (Application.isPlaying)
                    UnityEngine.Object.Destroy(_poolRoot.gameObject);
                else
                    UnityEngine.Object.DestroyImmediate(_poolRoot.gameObject);
                _poolRoot = null;
            }
        }
    }
}
