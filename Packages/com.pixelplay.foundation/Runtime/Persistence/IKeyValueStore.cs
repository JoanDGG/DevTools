namespace PixelPlay.Foundation.Persistence
{
    /// <summary>Provides typed access to a simple key-value store.</summary>
    public interface IKeyValueStore
    {
        /// <summary>Returns whether the key exists in the store.</summary>
        bool HasKey(string key);

        /// <summary>Reads an integer value or returns the supplied default.</summary>
        int GetInt(string key, int defaultValue = 0);

        /// <summary>Reads a float value or returns the supplied default.</summary>
        float GetFloat(string key, float defaultValue = 0f);

        /// <summary>Reads a string value or returns the supplied default.</summary>
        string GetString(string key, string defaultValue = "");

        /// <summary>Writes an integer value.</summary>
        void SetInt(string key, int value);

        /// <summary>Writes a float value.</summary>
        void SetFloat(string key, float value);

        /// <summary>Writes a string value.</summary>
        void SetString(string key, string value);

        /// <summary>Deletes a key from the store.</summary>
        void DeleteKey(string key);

        /// <summary>Flushes pending changes when supported by the implementation.</summary>
        void Save();
    }
}
