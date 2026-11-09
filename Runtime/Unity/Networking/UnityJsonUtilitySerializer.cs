using System;
using PixelPlay.Foundation.Networking;
using UnityEngine;

namespace PixelPlay.Foundation.Unity.Networking
{
    /// <summary>Implements the JSON serializer contract with Unity's JsonUtility.</summary>
    /// <remarks>JsonUtility works best with serializable DTO classes and does not support every .NET type.</remarks>
    public sealed class UnityJsonUtilitySerializer : IJsonSerializer
    {
        /// <inheritdoc />
        public string Serialize<T>(T value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            return JsonUtility.ToJson(value);
        }

        /// <inheritdoc />
        public T Deserialize<T>(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                throw new ArgumentException("JSON must not be null or whitespace.", nameof(json));

            return JsonUtility.FromJson<T>(json);
        }
    }
}
