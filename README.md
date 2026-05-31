# Demondragon Prefab Pooling

A lightweight, type-safe prefab object pool for Unity 6+.

## Features

- **Type-safe** — one `ObjectPool<T>` per unique prefab reference; type mismatches are caught at runtime with a clear error log.
- **Lifecycle callbacks** — `OnSpawned()` / `OnDespawned()` replace `Awake` / `OnDestroy` for pooled objects.
- **CancellationToken** — `DespawnToken` is refreshed on every spawn and cancelled on every despawn, so `Awaitable` async methods cancel cleanly.
- **Pre-warming** — `Prewarm<T>()` fills the pool during a loading screen to avoid first-frame allocation spikes.
- **Per-prefab config** — `PoolConfig` lets you set `DefaultCapacity` and `MaxSize` independently for each prefab.
- **Convenience Release** — call `myInstance.Release()` from anywhere without needing a reference to the manager.

## Installation

Add the package via **Window → Package Manager → Add package from git URL**:

```
https://github.com/jardaczk/unity-prefab-pooling.git#v1.0.0
```

## Quick Start

### 1 — Create a poolable component

```csharp
using Demondragon.PrefabPooling;
using UnityEngine;

public class Bullet : Poolable<Bullet>
{
	public override async void OnSpawned()
	{
		base.OnSpawned(); // activates the GameObject and refreshes DespawnToken
		await MoveAsync(DespawnToken);
	}

	public override void OnDespawned()
	{
		base.OnDespawned(); // cancels DespawnToken, stops coroutines, deactivates
	}

	private async Awaitable MoveAsync(System.Threading.CancellationToken ct)
	{
		while (!ct.IsCancellationRequested)
		{
			transform.position += Vector3.forward * Time.deltaTime * 10f;
			await Awaitable.NextFrameAsync(ct);
		}
	}
}
```

### 2 — Set up the manager

```csharp
using Demondragon.PrefabPooling;
using UnityEngine;

public class BulletSpawner : MonoBehaviour
{
	[SerializeField] private GameObject _bulletPrefab;

	private IPoolManager _pool;

	private void Awake()
	{
		_pool = new PoolManager();
		_pool.Prewarm<Bullet>(_bulletPrefab, 20); // optional warm-up
	}

	private void OnDestroy() => (_pool as System.IDisposable)?.Dispose();

	public void Fire()
	{
		Bullet b = _pool.Get<Bullet>(_bulletPrefab);
		// b is already active and OnSpawned() has been called
	}
}
```

### 3 — Return to pool

```csharp
// From the instance itself (simplest):
bullet.Release();

// Or via the manager:
_pool.Release(_bulletPrefab, bullet);
```

## API Reference

| Member | Description |
|--------|-------------|
| `PoolManager()` | Creates the manager and a `[PoolManager]` root GameObject. |
| `Get<T>(prefab, config)` | Retrieves or creates an instance and calls `OnSpawned`. |
| `Release<T>(prefab, instance)` | Returns an instance and calls `OnDespawned`. |
| `Prewarm<T>(prefab, count, config)` | Pre-populates the pool in an inactive state. |
| `Clear(prefab)` | Destroys all pooled instances for one prefab. |
| `ClearAll()` | Destroys all pooled instances across all pools. |
| `Dispose()` | Clears all pools and destroys the root GameObject. |
| `PoolConfig.Default` | `DefaultCapacity = 10`, `MaxSize = 100`. |

## License

MIT — see [LICENSE](LICENSE).
