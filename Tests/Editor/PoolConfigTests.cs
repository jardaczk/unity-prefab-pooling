using NUnit.Framework;

namespace Demondragon.PrefabPooling.Tests
{
    [TestFixture]
    internal sealed class PoolConfigTests
    {
        [Test]
        public void Default_HasExpectedValues()
        {
            var config = PoolConfig.Default;

            Assert.AreEqual(10, config.DefaultCapacity);
            Assert.AreEqual(100, config.MaxSize);
        }

        [Test]
        public void Constructor_StoresProvidedValues()
        {
            var config = new PoolConfig(defaultCapacity: 5, maxSize: 50);

            Assert.AreEqual(5, config.DefaultCapacity);
            Assert.AreEqual(50, config.MaxSize);
        }

        [Test]
        public void DefaultKeyword_HasZeroValues()
        {
            // When a caller passes `default`, MaxSize == 0 which the manager
            // uses as a sentinel to fall back to PoolConfig.Default.
            PoolConfig config = default;

            Assert.AreEqual(0, config.DefaultCapacity);
            Assert.AreEqual(0, config.MaxSize);
        }

        [Test]
        public void TwoDefaultInstances_AreEqual()
        {
            Assert.AreEqual(PoolConfig.Default, PoolConfig.Default);
        }
    }
}
