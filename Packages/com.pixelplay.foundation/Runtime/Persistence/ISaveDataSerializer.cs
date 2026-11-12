namespace PixelPlay.Foundation.Persistence
{
    /// <summary>Converts a save DTO to and from serialized text without choosing a JSON library.</summary>
    public interface ISaveDataSerializer
    {
        /// <summary>Serializes a save value.</summary>
        string Serialize<T>(T value);

        /// <summary>Deserializes a save value.</summary>
        T Deserialize<T>(string serializedValue);
    }
}
