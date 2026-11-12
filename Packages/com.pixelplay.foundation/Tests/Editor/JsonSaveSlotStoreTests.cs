using System;
using System.IO;
using NUnit.Framework;
using PixelPlay.Foundation.Persistence;

namespace PixelPlay.Foundation.Tests
{
    public sealed class JsonSaveSlotStoreTests
    {
        [Serializable]
        private sealed class SaveData
        {
            public int level;
        }

        private sealed class ReferenceSerializer : ISaveDataSerializer
        {
            private object _lastSerialized;
            public string Serialize<T>(T value) { _lastSerialized = value; return "test-payload"; }
            public T Deserialize<T>(string serializedValue) => (T)_lastSerialized;
        }

        private string _directory;
        private ReferenceSerializer _serializer;
        private JsonSaveSlotStore<SaveData> _store;

        [SetUp]
        public void SetUp()
        {
            _directory = Path.Combine(Path.GetTempPath(), "PixelPlay.Foundation.Tests", Guid.NewGuid().ToString("N"));
            _serializer = new ReferenceSerializer();
            _store = new JsonSaveSlotStore<SaveData>(_directory, _serializer);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_directory))
                Directory.Delete(_directory, true);
        }

        [Test]
        public void SaveAndTryLoad_PreserveSlotDataAndSchemaVersion()
        {
            var expected = new SaveData { level = 4 };
            _store.Save("player/one", expected, schemaVersion: 2);

            Assert.That(_store.HasSlot("player/one"), Is.True);
            Assert.That(_store.TryLoad("player/one", out SaveData actual, out int version), Is.True);
            Assert.That(actual.level, Is.EqualTo(4));
            Assert.That(version, Is.EqualTo(2));
            Assert.That(_store.GetSlotIds(), Is.EquivalentTo(new[] { "player/one" }));
        }

        [Test]
        public void MissingSlot_ReturnsFalseAndDeleteRemovesExistingSlot()
        {
            Assert.That(_store.TryLoad("missing", out SaveData missing, out int missingVersion), Is.False);
            Assert.That(missing, Is.Null);
            Assert.That(missingVersion, Is.Zero);

            _store.Save("main", new SaveData { level = 1 });
            _store.Delete("main");
            Assert.That(_store.HasSlot("main"), Is.False);
            Assert.That(_store.GetSlotIds(), Is.Empty);
        }
    }
}
