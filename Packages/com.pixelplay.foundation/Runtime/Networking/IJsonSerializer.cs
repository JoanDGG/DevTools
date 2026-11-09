namespace PixelPlay.Foundation.Networking
{
    /// <summary>Converts application data to and from JSON without choosing a JSON library.</summary>
    public interface IJsonSerializer
    {
        /// <summary>Serializes a value as JSON.</summary>
        string Serialize<T>(T value);

        /// <summary>Deserializes JSON into a value of the requested type.</summary>
        T Deserialize<T>(string json);
    }
}
