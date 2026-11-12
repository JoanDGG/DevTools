using System;
using System.Collections.Generic;
using NUnit.Framework;
using PixelPlay.Foundation.Debugging;
using PixelPlay.Foundation.Persistence;

namespace PixelPlay.Foundation.Tests
{
    public sealed class RuntimeTuningRegistryTests
    {
        private sealed class MemoryKeyValueStore : IKeyValueStore
        {
            private readonly Dictionary<string, object> _values = new Dictionary<string, object>();
            public int SaveCount { get; private set; }
            public bool HasKey(string key) => _values.ContainsKey(key);
            public int GetInt(string key, int defaultValue = 0) => _values.TryGetValue(key, out object value) ? (int)value : defaultValue;
            public float GetFloat(string key, float defaultValue = 0f) => _values.TryGetValue(key, out object value) ? (float)value : defaultValue;
            public string GetString(string key, string defaultValue = "") => _values.TryGetValue(key, out object value) ? (string)value : defaultValue;
            public void SetInt(string key, int value) => _values[key] = value;
            public void SetFloat(string key, float value) => _values[key] = value;
            public void SetString(string key, string value) => _values[key] = value;
            public void DeleteKey(string key) => _values.Remove(key);
            public void Save() => SaveCount++;
        }

        [Test]
        public void Tunables_EnforceBoundsAndResetToDefaults()
        {
            var registry = new RuntimeTuningRegistry();
            FloatTunable duration = registry.AddFloat("round.duration", "Duration", 30f, 5f, 60f);
            IntegerTunable rounds = registry.AddInteger("round.count", "Rounds", 3, 1, 10);
            BooleanTunable enabled = registry.AddBoolean("feature.enabled", "Feature", true);
            int changes = 0;
            duration.ValueChanged += _ => changes++;

            duration.SetValue(45f);
            rounds.SetValue(5);
            enabled.SetValue(false);
            registry.ResetAll();

            Assert.That(duration.Value, Is.EqualTo(30f));
            Assert.That(rounds.Value, Is.EqualTo(3));
            Assert.That(enabled.Value, Is.True);
            Assert.That(changes, Is.EqualTo(2));
            Assert.Throws<ArgumentOutOfRangeException>(() => duration.SetValue(61f));
        }

        [Test]
        public void SaveToAndLoadFrom_RoundTripTypedValuesAndSaveOnce()
        {
            var source = new RuntimeTuningRegistry();
            source.AddFloat("speed", "Speed", 2f, 0f, 10f).SetValue(7.5f);
            source.AddInteger("lives", "Lives", 3, 0, 9).SetValue(5);
            source.AddBoolean("enabled", "Enabled", false).SetValue(true);
            var store = new MemoryKeyValueStore();

            source.SaveTo(store, "test.");

            Assert.That(store.SaveCount, Is.EqualTo(1));
            var destination = new RuntimeTuningRegistry();
            FloatTunable speed = destination.AddFloat("speed", "Speed", 1f, 0f, 10f);
            IntegerTunable lives = destination.AddInteger("lives", "Lives", 1, 0, 9);
            BooleanTunable enabled = destination.AddBoolean("enabled", "Enabled", false);
            Assert.That(destination.LoadFrom(store, "test."), Is.EqualTo(3));
            Assert.That(speed.Value, Is.EqualTo(7.5f));
            Assert.That(lives.Value, Is.EqualTo(5));
            Assert.That(enabled.Value, Is.True);
        }
    }
}
