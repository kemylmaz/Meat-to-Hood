#if UNITY_EDITOR
using NUnit.Framework;

namespace ShawarmaTycoon.Tests
{
    public sealed class Phase2ArchitectureTests
    {
        private MemorySaveProvider provider;

        [SetUp]
        public void SetUp()
        {
            SaveRepository.ResetStateForTests();
            provider = new MemorySaveProvider();
            SaveRepository.InitializeForTests(provider);
        }

        [TearDown]
        public void TearDown() => SaveRepository.ResetStateForTests();

        [Test]
        public void SaveData_OverwritesTypedValuesWithoutDuplicatingKeys()
        {
            SaveData data = new();
            data.SetInt("coins", 10);
            data.SetInt("coins", 25);
            data.SetLong("last_seen", 1234567890123L);

            Assert.That(data.TryGetInt("coins", out int coins), Is.True);
            Assert.That(coins, Is.EqualTo(25));
            Assert.That(data.ints.Count, Is.EqualTo(1));
            Assert.That(data.TryGetLong("last_seen", out long lastSeen), Is.True);
            Assert.That(lastSeen, Is.EqualTo(1234567890123L));
        }

        [Test]
        public void SaveRepository_DefersWritesUntilAFlushCheckpoint()
        {
            SaveRepository.SetInt("coins", 70);
            SaveRepository.SetInt("coins", 90);

            Assert.That(SaveRepository.IsDirty, Is.True);
            Assert.That(provider.SaveCalls, Is.Zero);

            SaveRepository.FlushNow();

            Assert.That(provider.SaveCalls, Is.EqualTo(1));
            Assert.That(provider.Data.TryGetInt("coins", out int coins), Is.True);
            Assert.That(coins, Is.EqualTo(90));
            Assert.That(SaveRepository.IsDirty, Is.False);
        }

        [Test]
        public void RuntimeDefaults_AreMobileSafe()
        {
            GameConfig config = GameConfig.CreateRuntimeDefaults();
            Assert.That(config.PortraitOnly, Is.True);
            Assert.That(config.RunInBackground, Is.False);
            Assert.That(config.KeepScreenAwake, Is.False);
            Assert.That(config.SaveFlushIntervalSeconds, Is.GreaterThanOrEqualTo(2f));
        }

        private sealed class MemorySaveProvider : ISaveProvider
        {
            public int SaveCalls { get; private set; }
            public SaveData Data { get; private set; }

            public bool TryLoad(out SaveData data)
            {
                data = Data;
                return data != null;
            }

            public void Save(SaveData data)
            {
                SaveCalls++;
                Data = data;
            }

            public void Delete() => Data = null;
        }
    }
}
#endif


