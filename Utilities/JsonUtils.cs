using System.Text.Json;

namespace Onset_Serialization.Utilities
{
    public class JsonUtils
    {
        public static string Serialize<T>(T obj)
        {
            return JsonSerializer.Serialize(obj, typeof(T), new JsonSerializerOptions()
            {
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            });
        }

        public static T Deserialize<T>(string json) => JsonSerializer.Deserialize<T>(json);
    }
}
