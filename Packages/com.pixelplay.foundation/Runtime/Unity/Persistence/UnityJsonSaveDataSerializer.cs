using System;
using PixelPlay.Foundation.Persistence;
using UnityEngine;

namespace PixelPlay.Foundation.Unity.Persistence
{
    /// <summary>Implements the save serializer contract with Unity's JsonUtility.</summary>
    /// <remarks>JsonUtility supports serializable DTO classes but not every .NET type.</remarks>
    public sealed class UnityJsonSaveDataSerializer : ISaveDataSerializer
    {
        /// <inheritdoc />
        public string Serialize<T>(T value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));
            return JsonUtility.ToJson(value);
        }

        /// <inheritdoc />
        public T Deserialize<T>(string serializedValue)
        {
            if (string.IsNullOrWhiteSpace(serializedValue))
                throw new ArgumentException("Serialized save data must not be empty.", nameof(serializedValue));
            return JsonUtility.FromJson<T>(serializedValue);
        }
    }
}
