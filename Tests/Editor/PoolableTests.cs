using System.Threading;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Demondragon.PrefabPooling.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="Poolable{T}"/> lifecycle callbacks,
    /// DespawnToken management, and fallback Release behaviour.
    /// </summary>
    [TestFixture]
    internal sealed class PoolableTests
    {
        private PoolManager _pool;

        // ── Test double ──────────────────────────────────────────────────────

        private sealed class TrackedBehaviour : Poolable<TrackedBehaviour>
        {
            public int SpawnCount;
            public int DespawnCount;
            public CancellationToken LastDespawnToken;

            public override void OnSpawned()
            {
                SpawnCount++;
                base.OnSpawned();
                LastDespawnToken = DespawnToken;
            }

            public override void OnDespawned()
            {
                DespawnCount++;
                base.OnDespawned();
            }
        }

        // ── Setup / teardown ─────────────────────────────────────────────────

        [SetUp]
        public void SetUp() => _pool = new PoolManager();

        [TearDown]
        public void TearDown() => _pool.Dispose();

        private static GameObject MakePrefab()
        {
            var go = new GameObject("[Prefab_Tracked]");
            go.AddComponent<TrackedBehaviour>();
            go.SetActive(false);
            return go;
        }

        // ── OnSpawned / OnDespawned ──────────────────────────────────────────

        [Test]
        public void OnSpawned_CalledOnGet()
        {
            var prefab = MakePrefab();
            var instance = _pool.Get<TrackedBehaviour>(prefab);

            Assert.AreEqual(1, instance.SpawnCount);

            Object.DestroyImmediate(prefab);
        }

        [Test]
        public void OnDespawned_CalledOnRelease()
        {
            var prefab = MakePrefab();
            var instance = _pool.Get<TrackedBehaviour>(prefab);
            _pool.Release(prefab, instance);

            Assert.AreEqual(1, instance.DespawnCount);

            Object.DestroyImmediate(prefab);
        }

        [Test]
        public void SpawnDespawn_CalledCorrectly_AcrossMultipleCycles()
        {
            var prefab = MakePrefab();
            var instance = _pool.Get<TrackedBehaviour>(prefab);
            _pool.Release(prefab, instance);

            instance = _pool.Get<TrackedBehaviour>(prefab);
            _pool.Release(prefab, instance);

            Assert.AreEqual(2, instance.SpawnCount);
            Assert.AreEqual(2, instance.DespawnCount);

            Object.DestroyImmediate(prefab);
        }

        // ── DespawnToken ─────────────────────────────────────────────────────

        [Test]
        public void DespawnToken_NotCancelled_WhileSpawned()
        {
            var prefab = MakePrefab();
            var instance = _pool.Get<TrackedBehaviour>(prefab);

            Assert.IsFalse(instance.LastDespawnToken.IsCancellationRequested);

            Object.DestroyImmediate(prefab);
        }

        [Test]
        public void DespawnToken_Cancelled_AfterRelease()
        {
            var prefab = MakePrefab();
            var instance = _pool.Get<TrackedBehaviour>(prefab);
            var token = instance.LastDespawnToken;

            _pool.Release(prefab, instance);

            Assert.IsTrue(token.IsCancellationRequested);

            Object.DestroyImmediate(prefab);
        }

        [Test]
        public void DespawnToken_IsNew_AfterRespawn()
        {
            var prefab = MakePrefab();
            var instance = _pool.Get<TrackedBehaviour>(prefab);
            var firstToken = instance.LastDespawnToken;

            _pool.Release(prefab, instance);
            _pool.Get<TrackedBehaviour>(prefab);
            var secondToken = instance.LastDespawnToken;

            Assert.IsFalse(secondToken.IsCancellationRequested);
            Assert.AreNotEqual(firstToken, secondToken);

            Object.DestroyImmediate(prefab);
        }

        // ── Release convenience method ───────────────────────────────────────

        [Test]
        public void Release_ViaInstance_ReturnsToPool()
        {
            var prefab = MakePrefab();
            var instance = _pool.Get<TrackedBehaviour>(prefab);
            instance.Release();

            Assert.IsFalse(instance.gameObject.activeSelf);

            Object.DestroyImmediate(prefab);
        }

        [Test]
        public void Release_WithoutPoolManager_DestroysObject()
        {
            var go = new GameObject("[Standalone]");
            var standalone = go.AddComponent<TrackedBehaviour>();

            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex(
                System.Text.RegularExpressions.Regex.Escape("[Poolable] [Standalone] has no PoolManager reference. Destroying instead.")));
            standalone.Release();

            Assert.IsTrue(standalone == null || standalone.gameObject == null);
        }

        // ── SetPoolData ──────────────────────────────────────────────────────

        [Test]
        public void SetPoolData_AssignsManagerAndPrefab()
        {
            var prefab = MakePrefab();
            var instance = _pool.Get<TrackedBehaviour>(prefab);

            Assert.AreSame(_pool, instance.PoolManager);
            Assert.AreSame(prefab, instance.PoolPrefab);

            Object.DestroyImmediate(prefab);
        }
    }
}
