# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0] - 2025-01-01

### Added
- `IPoolManager` / `PoolManager` — central pool manager with per-prefab `ObjectPool<T>`.
- `IPoolable<T>` / `Poolable<T>` — CRTP base class with `OnSpawned`, `OnDespawned`, and `Release`.
- `PoolConfig` — immutable capacity config struct with `PoolConfig.Default` (10 / 100).
- `DespawnToken` — `CancellationToken` refreshed on every spawn, cancelled on every despawn.
- `Prewarm<T>` — pre-populates a pool during a loading screen.
- EditMode test suite (29 tests) covering all public API paths.
