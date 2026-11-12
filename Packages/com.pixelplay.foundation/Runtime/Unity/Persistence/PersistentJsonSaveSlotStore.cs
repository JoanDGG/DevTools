using System.Collections.Generic;
using System.IO;
using PixelPlay.Foundation.Persistence;
using UnityEngine;

namespace PixelPlay.Foundation.Unity.Persistence
{
    /// <summary>Convenience wrapper for Unity-serialized JSON save slots under the persistent data path.</summary>
    /// <remarks>
    /// <typeparamref name="T"/> should be a plain serializable DTO class, not a MonoBehaviour or ScriptableObject.
    /// Unity JSON serialization stores supported fields (not properties), including primitive values, strings,
    /// enums, serializable nested classes/structs, and arrays or lists of supported field types. It does not
    /// support dictionaries, interfaces, arbitrary polymorphism, or nested/multidimensional collections.
    /// Wrap root primitives and arrays in a serializable class. Avoid UnityEngine.Object references in persistent
    /// data: their instance IDs do not identify assets or scene objects across application sessions.
    /// </remarks>
    public sealed class PersistentJsonSaveSlotStore<T> where T : class
    {
        private readonly JsonSaveSlotStore<T> _store;

        /// <summary>Creates a namespaced save store beneath <see cref="Application.persistentDataPath"/>.</summary>
        public PersistentJsonSaveSlotStore(string folderName)
        {
            if (string.IsNullOrWhiteSpace(folderName))
                throw new System.ArgumentException("A folder name is required.", nameof(folderName));

            _store = new JsonSaveSlotStore<T>(Path.Combine(Application.persistentDataPath, folderName),
                new UnityJsonSaveDataSerializer());
        }

        /// <inheritdoc cref="JsonSaveSlotStore{T}.Save" />
        public void Save(string slotId, T data, int schemaVersion = 1) => _store.Save(slotId, data, schemaVersion);

        /// <inheritdoc cref="JsonSaveSlotStore{T}.TryLoad" />
        public bool TryLoad(string slotId, out T data, out int schemaVersion) =>
            _store.TryLoad(slotId, out data, out schemaVersion);

        /// <inheritdoc cref="JsonSaveSlotStore{T}.HasSlot" />
        public bool HasSlot(string slotId) => _store.HasSlot(slotId);

        /// <inheritdoc cref="JsonSaveSlotStore{T}.Delete" />
        public void Delete(string slotId) => _store.Delete(slotId);

        /// <inheritdoc cref="JsonSaveSlotStore{T}.GetSlotIds" />
        public IReadOnlyList<string> GetSlotIds() => _store.GetSlotIds();
    }
}
