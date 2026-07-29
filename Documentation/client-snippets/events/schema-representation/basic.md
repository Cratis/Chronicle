```csharp
using System.Text.Json;
using System.Text.Json.Serialization;
using Cratis.Chronicle.Schemas;

[JsonSchemaType(typeof(string))]
[JsonConverter(typeof(PostalCodeJsonConverter))]
public class PostalCode(string value)
{
    public string Value { get; } = value;
}

public class PostalCodeJsonConverter : JsonConverter<PostalCode>
{
    public override PostalCode Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        new(reader.GetString()!);

    public override void Write(Utf8JsonWriter writer, PostalCode value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value.Value);
}
```
