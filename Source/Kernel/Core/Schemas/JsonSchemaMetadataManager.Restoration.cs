// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;

namespace Cratis.Chronicle.Schemas;

public partial class JsonSchemaMetadataManager
{
    static JsonSchema RestorationSchema(JsonSchema schema)
    {
        var reach = schema.MetadataReferences;
        if (!reach.Contextual) return schema.ActualTypeSchema;

        var type = StrictJsonSchemaRelease.ProtectedType(schema);
        return reach.Schemas.FirstOrDefault(target => (target.Type & ~JsonObjectType.Null) == type)?.ActualTypeSchema
            ?? schema.ActualTypeSchema;
    }

    static JsonNode? RestoreReleasedContainerShape(JsonNode released, JsonSchema propertySchema)
    {
        propertySchema = RestorationSchema(propertySchema);
        if (released is not JsonValue scalar || !scalar.TryGetValue<string>(out var text)) return released;

        // The ciphertext format stores scalar text without a JSON type tag. The schema supplies the
        // missing kind; strings must never be parsed just because their content resembles JSON.
        if (HasNonStringScalarType(propertySchema))
        {
            return RestoreReleasedScalar(released, text, propertySchema);
        }

        // A coarse schema metadata marker on a whole container is blob-encrypted to a single ciphertext string,
        // even though its schema type stays array (a collection) or object (a value object). Releasing it
        // decrypts back to the original JSON text; re-parse that text into the container the schema expects so
        // the read model round-trips into its collection or value-object type rather than a raw string (which
        // fails to deserialize). Genuine string scalars are left untouched.
        var isContainer = propertySchema.IsArray || propertySchema.Type.HasFlag(JsonObjectType.Object);
        return isContainer ? RestoreReleasedContainer(released, text, propertySchema) : released;
    }

    static bool HasNonStringScalarType(JsonSchema schema) =>
        !schema.Type.HasFlag(JsonObjectType.String) &&
        (schema.Type & (JsonObjectType.Integer | JsonObjectType.Number | JsonObjectType.Boolean)) != JsonObjectType.None;

    static JsonNode? RestoreReleasedScalar(JsonNode released, string text, JsonSchema schema)
    {
        if (string.IsNullOrEmpty(text))
        {
            return EmptyReleasedScalar(schema);
        }

        // Enum names and legacy scalar text must reach the converter unchanged when they do not
        // match the declared JSON kind. Never pass decrypted parser diagnostics to the release log.
        return TryParseReleasedScalar(text, schema, out var parsed) ? parsed : released;
    }

    static JsonNode? EmptyReleasedScalar(JsonSchema schema)
    {
        if (schema.Type.HasFlag(JsonObjectType.Null) || (schema.Format?.EndsWith('?') ?? false))
        {
            return null;
        }

        // A parsed JSON number supports every schema numeric format, unlike a JsonValue<int>
        // which cannot be consumed as a double, long or decimal by the converter.
        return schema.Type.HasFlag(JsonObjectType.Boolean) ? JsonValue.Create(false) : JsonNode.Parse("0");
    }

    static JsonNode RestoreReleasedContainer(JsonNode released, string text, JsonSchema schema)
    {
        // When the subject's encryption key has been crypto-shredded (GDPR right-to-erasure), the handler
        // surfaces the erased value as an empty string. An erased container reads as empty, so return an
        // empty container rather than letting JsonNode.Parse(string.Empty) throw and poison the release path.
        if (string.IsNullOrWhiteSpace(text))
        {
            return schema.IsArray ? new JsonArray() : new JsonObject();
        }

        return JsonNode.Parse(text) ?? released;
    }

    static bool TryParseReleasedScalar(string text, JsonSchema schema, out JsonNode? parsed)
    {
        try
        {
            parsed = JsonNode.Parse(text);
            var kind = parsed?.GetValueKind();
            return schema.Type.HasFlag(JsonObjectType.Boolean)
                ? kind is JsonValueKind.True or JsonValueKind.False
                : kind == JsonValueKind.Number;
        }
        catch (JsonException)
        {
            // Released text is not a JSON scalar. Preserve it for legacy conversion without exposing
            // the parser's value-bearing exception message or substituting a fabricated default.
            parsed = null;
            return false;
        }
    }
}
