using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Demondragon.PrefabPooling.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="PoolManager"/>.
    /// </summary>
    [TestFixture]
    internal sealed class PoolManagerTests
    {
        private PoolManager _pool;

        private sealed class TestBehaviour : Poolable<TestBehaviour> { }
        private sealed class OtherBehaviour : Poolable<OtherBehaviour> { }

        [SetUp]
        public void SetUp() => _pool = new PoolManager();

        [TearDown]
        public void TearDown() => _pool.Dispose();

        private static GameObject MakePrefab<T>() where T : MonoBehaviour
        {
            var go = new GameObject($"[Prefab_{typeof(T).Name}]");
            go.AddComponent<T>();
            go.SetActive(false);
            return go;
        }

        // ── Get ──────────────────────────────────────────────────────────────

        [Test]
        public void Get_ReturnNonNull_WhenPrefabValid()
        {
            var prefab = MakePrefab<TestBehaviour>();
            var instance = _pool.Get<TestBehaviour>(prefab);

            Assert.IsNotNull(instance);

            Object.DestroyImmediate(prefab);
        }

        [Test]
        public void Get_ActivatesInstance()
        {
            var prefab = MakePrefab<TestBehaviour>();
            var instance = _pool.Get<TestBehaviour>(prefab);

            Assert.IsTrue(instance.gameObject.activeSelf);

            Object.DestroyImmediate(prefab);
        }

        [Test]
        public void Get_ReturnsNewInstance_WhenNoneInPool()
        {
            var prefab = MakePrefab<TestBehaviour>();
            var a = _pool.Get<TestBehaviour>(prefab);
            var b = _pool.Get<TestBehaviour>(prefab);

            Assert.AreNotSame(a, b);

            Object.DestroyImmediate(prefab);
        }

        [Test]
        public void Get_ReusesReleasedInstance()
        {
            var prefab = MakePrefab<TestBehaviour>();
            var a = _pool.Get<TestBehaviour>(prefab);
            _pool.Release(prefab, a);
            var b = _pool.Get<TestBehaviour>(prefab);

            Assert.AreSame(a, b);

            Object.DestroyImmediate(prefab);
        }

        [Test]
        public void Get_ReturnsNull_WhenPrefabIsNull()
        {
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex(
                System.Text.RegularExpressions.Regex.Escape("[PoolManager] Prefab is null")));
            var result = _pool.Get<TestBehaviour>(null);

            Assert.IsNull(result);
        }

        [Test]
        public void Get_ReturnsNull_WhenTypeMismatch()
        {
            var prefab = MakePrefab<TestBehaviour>();
            _pool.Get<TestBehaviour>(prefab);

            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex(
                System.Text.RegularExpressions.Regex.Escape("[PoolManager] Type mismatch for prefab '[Prefab_TestBehaviour]': pool holds TestBehaviour, requested OtherBehaviour")));
            var mismatchResult = _pool.Get<OtherBehaviour>(prefab);

            Assert.IsNull(mismatchResult);

            Object.DestroyImmediate(prefab);
        }

        [Test]
        public void Get_ReturnsNull_AfterDispose()
        {
            var prefab = MakePrefab<TestBehaviour>();
            _pool.Dispose();

            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex(
                System.Text.RegularExpressions.Regex.Escape("[PoolManager] Cannot get from disposed PoolManager")));
            var result = _pool.Get<TestBehaviour>(prefab);

            Assert.IsNull(result);

            Object.DestroyImmediate(prefab);
        }

        // ── Release ──────────────────────────────────────────────────────────

        [Test]
        public void Release_DeactivatesInstance()
        {
            var prefab = MakePrefab<TestBehaviour>();
            var instance = _pool.Get<TestBehaviour>(prefab);
            _pool.Release(prefab, instance);

            Assert.IsFalse(instance.gameObject.activeSelf);

            Object.DestroyImmediate(prefab);
        }

        [Test]
        public void Release_ReparentsUnderPoolRoot()
        {
            var prefab = MakePrefab<TestBehaviour>();
            var instance = _pool.Get<TestBehaviour>(prefab);

            var tempParent = new GameObject("TempParent");
            instance.transform.SetParent(tempParent.transform);

            _pool.Release(prefab, instance);

            Assert.AreEqual("[PoolManager]", instance.transform.parent.gameObject.name);

            Object.DestroyImmediate(prefab);
            Object.DestroyImmediate(tempParent);
        }

        [Test]
        public void Release_NullInstance_DoesNotThrow()
        {
            var prefab = MakePrefab<TestBehaviour>();

            Assert.DoesNotThrow(() => _pool.Release<TestBehaviour>(prefab, null));

            Object.DestroyImmediate(prefab);
        }

        // ── Prewarm ──────────────────────────────────────────────────────────

        [Test]
        public void Prewarm_CreatesInactiveInstances()
        {
            var prefab = MakePrefab<TestBehaviour>();
            _pool.Prewarm<TestBehaviour>(prefab, 3);

            var instances = new List<TestBehaviour>();
            for (int i = 0; i < 3; i++)
                instances.Add(_pool.Get<TestBehaviour>(prefab));

            var extra = _pool.Get<TestBehaviour>(prefab);
            Assert.IsNotNull(extra);
            Assert.IsFalse(instances.Contains(extra));

            Object.DestroyImmediate(prefab);
        }

        [Test]
        public void Prewarm_ZeroCount_DoesNotThrow()
        {
            var prefab = MakePrefab<TestBehaviour>();

            Assert.DoesNotThrow(() => _pool.Prewarm<TestBehaviour>(prefab, 0));

            Object.DestroyImmediate(prefab);
        }

        // ── Clear / ClearAll ─────────────────────────────────────────────────

        [Test]
        public void Clear_RemovesPool_NewGetCreatesNewInstance()
        {
            var prefab = MakePrefab<TestBehaviour>();
            var a = _pool.Get<TestBehaviour>(prefab);
            _pool.Release(prefab, a);

            _pool.Clear(prefab);

            var b = _pool.Get<TestBehaviour>(prefab);
            Assert.AreNotSame(a, b);

            Object.DestroyImmediate(prefab);
        }

        [Test]
        public void Clear_NullPrefab_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _pool.Clear(null));
        }

        [Test]
        public void ClearAll_SubsequentGetCreatesNewInstance()
        {
            var prefab = MakePrefab<TestBehaviour>();
            var a = _pool.Get<TestBehaviour>(prefab);
            _pool.Release(prefab, a);

            _pool.ClearAll();

            var b = _pool.Get<TestBehaviour>(prefab);
            Assert.AreNotSame(a, b);

            Object.DestroyImmediate(prefab);
        }

        // ── PoolConfig ───────────────────────────────────────────────────────

        [Test]
        public void Get_WithCustomConfig_UsesProvidedCapacity()
        {
            var prefab = MakePrefab<TestBehaviour>();
            var config = new PoolConfig(defaultCapacity: 2, maxSize: 2);

            var a = _pool.Get<TestBehaviour>(prefab, config);
            var b = _pool.Get<TestBehaviour>(prefab, config);
            var c = _pool.Get<TestBehaviour>(prefab, config);

            _pool.Release(prefab, a);
            _pool.Release(prefab, b);
            _pool.Release(prefab, c); // c destroyed — pool is full at 2

            Assert.IsTrue(c == null || c.gameObject == null);

            Object.DestroyImmediate(prefab);
        }
    }
}
