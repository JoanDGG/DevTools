using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace PixelPlay.Foundation.Persistence
{
    /// <summary>Stores serializable game snapshots as independent JSON save slots.</summary>
    /// <remarks>Slot IDs are encoded into safe filenames. Writes use a temporary file before replacing the slot.</remarks>
    public sealed class JsonSaveSlotStore<T> where T : class
    {
        [Serializable]
        private sealed class SaveEnvelope
        {
            public int schemaVersion;
            public T data;
        }

        private readonly string _directoryPath;
        private readonly ISaveDataSerializer _serializer;

        /// <summary>Creates a store rooted at the given directory using the supplied save serializer.</summary>
        /// <param name="directoryPath">Directory in which slot files are stored.</param>
        /// <param name="serializer">Serializer used for save envelopes and payloads.</param>
        public JsonSaveSlotStore(string directoryPath, ISaveDataSerializer serializer)
        {
            if (string.IsNullOrWhiteSpace(directoryPath))
                throw new ArgumentException("A save directory is required.", nameof(directoryPath));
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _directoryPath = Path.GetFullPath(directoryPath);
        }

        /// <summary>Saves data to a slot, replacing any previous contents.</summary>
        /// <param name="slotId">Logical slot name; encoded to a filename-safe representation.</param>
        /// <param name="data">Serializable snapshot to store.</param>
        /// <param name="schemaVersion">Positive payload version for application-managed migrations.</param>
        public void Save(string slotId, T data, int schemaVersion = 1)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            if (schemaVersion < 1)
                throw new ArgumentOutOfRangeException(nameof(schemaVersion), "Schema version must be positive.");

            string destination = GetSlotPath(slotId);
            string temporary = destination + ".tmp";
            Directory.CreateDirectory(_directoryPath);
            try
            {
                string json = _serializer.Serialize(new SaveEnvelope { schemaVersion = schemaVersion, data = data });
                File.WriteAllText(temporary, json, new UTF8Encoding(false));
                if (File.Exists(destination))
                    File.Replace(temporary, destination, null);
                else
                    File.Move(temporary, destination);
            }
            finally
            {
                if (File.Exists(temporary))
                    File.Delete(temporary);
            }
        }

        /// <summary>Loads a slot and returns its schema version; returns false when the slot does not exist.</summary>
        /// <param name="slotId">Logical slot name used when saving.</param>
        /// <param name="data">Loaded snapshot, or null when the slot is missing.</param>
        /// <param name="schemaVersion">Stored version, or zero when the slot is missing.</param>
        /// <returns>True when a valid slot was loaded.</returns>
        public bool TryLoad(string slotId, out T data, out int schemaVersion)
        {
            string path = GetSlotPath(slotId);
            if (!File.Exists(path))
            {
                data = null;
                schemaVersion = 0;
                return false;
            }

            string json = File.ReadAllText(path, Encoding.UTF8);
            SaveEnvelope envelope = _serializer.Deserialize<SaveEnvelope>(json);
            if (envelope == null || envelope.data == null || envelope.schemaVersion < 1)
                throw new InvalidDataException($"Save slot '{slotId}' has an invalid or unsupported envelope.");

            data = envelope.data;
            schemaVersion = envelope.schemaVersion;
            return true;
        }

        /// <summary>Returns whether a slot exists.</summary>
        /// <param name="slotId">Logical slot name to look up.</param>
        public bool HasSlot(string slotId) => File.Exists(GetSlotPath(slotId));

        /// <summary>Deletes a slot if it exists.</summary>
        /// <param name="slotId">Logical slot name to delete.</param>
        public void Delete(string slotId)
        {
            string path = GetSlotPath(slotId);
            if (File.Exists(path))
                File.Delete(path);
        }

        /// <summary>Returns the IDs of all valid slots, sorted ordinally.</summary>
        public IReadOnlyList<string> GetSlotIds()
        {
            var ids = new List<string>();
            if (!Directory.Exists(_directoryPath))
                return ids.AsReadOnly();

            string[] paths = Directory.GetFiles(_directoryPath, "slot_*.json", SearchOption.TopDirectoryOnly);
            for (int i = 0; i < paths.Length; i++)
            {
                string fileName = Path.GetFileNameWithoutExtension(paths[i]);
                string encoded = fileName.Substring("slot_".Length);
                try
                {
                    ids.Add(Encoding.UTF8.GetString(FromHex(encoded)));
                }
                catch (FormatException)
                {
                    // Ignore files that do not follow this store's slot naming convention.
                }
            }

            ids.Sort(StringComparer.Ordinal);
            return ids.AsReadOnly();
        }

        private string GetSlotPath(string slotId)
        {
            if (string.IsNullOrWhiteSpace(slotId))
                throw new ArgumentException("A slot ID must not be null or whitespace.", nameof(slotId));
            byte[] bytes = Encoding.UTF8.GetBytes(slotId);
            var hex = new StringBuilder(bytes.Length * 2);
            for (int i = 0; i < bytes.Length; i++)
                hex.Append(bytes[i].ToString("x2"));
            return Path.Combine(_directoryPath, "slot_" + hex + ".json");
        }

        private static byte[] FromHex(string value)
        {
            if (value.Length == 0 || value.Length % 2 != 0)
                throw new FormatException("Invalid slot filename.");
            var bytes = new byte[value.Length / 2];
            for (int i = 0; i < bytes.Length; i++)
                bytes[i] = Convert.ToByte(value.Substring(i * 2, 2), 16);
            return bytes;
        }
    }
}
