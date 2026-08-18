#if UNITY_EDITOR
using NUnit.Framework;
using UnityEngine;

namespace ShawarmaTycoon.Tests
{
    public sealed class Phase2RuntimeSmokeTests
    {
        private GameObject bootstrapHost;

        [OneTimeSetUp]
        public void BuildPrototypeForFixture()
        {
            if (GameObject.Find("Shawarma Prototype Runtime") != null) return;
            bootstrapHost = new GameObject("Phase 2 Test Bootstrap");
            bootstrapHost.AddComponent<ShawarmaPrototypeBootstrap>();
        }

        [OneTimeTearDown]
        public void TearDownFixture()
        {
            GameObject runtime = GameObject.Find("Shawarma Prototype Runtime");
            if (runtime != null) Object.DestroyImmediate(runtime);
            if (bootstrapHost != null) Object.DestroyImmediate(bootstrapHost);
        }

        [Test]
        public void Prototype_InstallsPhase2RuntimeServices()
        {
            Assert.That(GameObject.Find("Shawarma Prototype Runtime"), Is.Not.Null);
            Assert.That(GameEconomy.Instance, Is.Not.Null);
            Assert.That(GameplayObjectPool.Instance, Is.Not.Null);
            Assert.That(Object.FindFirstObjectByType<GameSessionPersistence>(), Is.Not.Null);
        }

        [Test]
        public void GameplayPool_ReusesReleasedVisuals()
        {
            GameplayObjectPool pool = GameplayObjectPool.Instance;
            Assert.That(pool, Is.Not.Null);

            GameObject first = GameplayObjectPool.Rent(
                "test.phase2.reuse", pool.transform, () => new GameObject("Pooled Test Visual"));
            GameplayObjectPool.Release(first);
            GameObject second = GameplayObjectPool.Rent(
                "test.phase2.reuse", pool.transform, () => new GameObject("Unexpected Allocation"));

            Assert.That(second, Is.SameAs(first));
            GameplayObjectPool.Release(second);
        }
    }
}
#endif
