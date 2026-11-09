using System;
using PixelPlay.Foundation.Persistence;
using UnityEngine;

namespace PixelPlay.Foundation.Unity.Persistence
{
    /// <summary>Stores typed local values through Unity's PlayerPrefs API.</summary>
    /// <remarks>Use this for local preferences, not secrets or tamper-proof data.</remarks>
    public sealed class PlayerPrefsKeyValueStore : IKeyValueStore
    {
        private readonly string _keyPrefix;

        /// <summary>Creates a PlayerPrefs store with an optional namespace prefix.</summary>
        public PlayerPrefsKeyValueStore(string keyPrefix = "")
        {
            _keyPrefix = keyPrefix ?? string.Empty;
        }

        /// <inheritdoc />
        public bool HasKey(string key) => PlayerPrefs.HasKey(BuildKey(key));

        /// <inheritdoc />
        public int GetInt(string key, int defaultValue = 0) =>
            PlayerPrefs.GetInt(BuildKey(key), defaultValue);

        /// <inheritdoc />
        public float GetFloat(string key, float defaultValue = 0f) =>
            PlayerPrefs.GetFloat(BuildKey(key), defaultValue);

        /// <inheritdoc />
        public string GetString(string key, string defaultValue = "") =>
            PlayerPrefs.GetString(BuildKey(key), defaultValue);

        /// <inheritdoc />
        public void SetInt(string key, int value) => PlayerPrefs.SetInt(BuildKey(key), value);

        /// <inheritdoc />
        public void SetFloat(string key, float value) => PlayerPrefs.SetFloat(BuildKey(key), value);

        /// <inheritdoc />
        public void SetString(string key, string value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            PlayerPrefs.SetString(BuildKey(key), value);
        }

        /// <inheritdoc />
        public void DeleteKey(string key) => PlayerPrefs.DeleteKey(BuildKey(key));

        /// <inheritdoc />
        public void Save() => PlayerPrefs.Save();

        private string BuildKey(string key)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("A key must not be null or empty.", nameof(key));

            return _keyPrefix + key;
        }
    }
}
