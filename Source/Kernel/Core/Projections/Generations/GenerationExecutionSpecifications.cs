// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Concepts.Projections.Generations;
using Cratis.Chronicle.Concepts.Projections.Json;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Properties;
using Cratis.Json;

namespace Cratis.Chronicle.Projections.Generations;

/// <summary>
/// Captures versioned execution inputs without accepting operational sink configuration or credentials.
/// </summary>
/// <param name="options">The kernel's definition serialization options.</param>
public sealed class GenerationExecutionSpecifications(JsonSerializerOptions options)
{
    readonly JsonSerializerOptions _options = CreateOptions(options);

    /// <summary>
    /// Canonicalizes JSON recursively using ordinal property ordering while preserving array order.
    /// </summary>
    /// <param name="content">The execution JSON.</param>
    /// <returns>The version-one canonical JSON.</returns>
    public static string Canonicalize(string content)
    {
        using var document = JsonDocument.Parse(content);
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            Write(writer, document.RootElement);
        }
        return Encoding.UTF8.GetString(stream.ToArray());
    }

    /// <summary>
    /// Snapshots definitions, schemas, delivery interpretation and semantic sink revision.
    /// </summary>
    /// <param name="projection">The complete projection definition.</param>
    /// <param name="readModel">The target definition, schemas and indexes.</param>
    /// <param name="eventSchemas">The relevant source schemas.</param>
    /// <param name="semanticSinkRevision">The credential-free semantic sink configuration revision.</param>
    /// <returns>Immutable execution inputs.</returns>
    /// <exception cref="InvalidMaterializationExecution">The execution scope or semantic revision is unsupported.</exception>
    public MaterializationExecutionSpecification Capture(ProjectionDefinition projection, ReadModelDefinition readModel, IReadOnlyCollection<EventTypeSchema> eventSchemas, MaterializationRevision semanticSinkRevision)
    {
        if (projection.Scope != ProjectionScope.Namespaced) throw new InvalidMaterializationExecution();
        if (projection.HasLegacyStorageFields) throw new InvalidMaterializationExecution();
        if (semanticSinkRevision == MaterializationRevision.Unknown) throw new InvalidMaterializationExecution();
        var model = CaptureModel(readModel);
        var sourceSchemas = CaptureEventSchemas(eventSchemas);
        var content = JsonSerializer.Serialize(new { CanonicalizationVersion = 1, InterpretationVersion = 1, Projection = projection, ReadModel = model, EventSchemas = sourceSchemas, SemanticSinkRevision = semanticSinkRevision }, _options);
        return MaterializationExecutionSpecification.Capture(projection.EventSequenceId, Canonicalize(content), projection.Version);
    }

    static JsonSerializerOptions CreateOptions(JsonSerializerOptions options)
    {
        var captured = new JsonSerializerOptions(options);
        captured.Converters.Insert(0, new PropertyPathJsonConverter());
        captured.Converters.Insert(0, new PropertyExpressionDictionaryConverter());
        captured.Converters.Insert(0, new PropertyPathChildrenDefinitionDictionaryJsonConverter());
        captured.Converters.Insert(0, new ExactEventDictionary<FromDefinition>());
        captured.Converters.Insert(0, new ExactEventDictionary<JoinDefinition>());
        captured.Converters.Insert(0, new ExactEventDictionary<RemovedWithDefinition>());
        captured.Converters.Insert(0, new ExactEventDictionary<RemovedWithJoinDefinition>());
        return captured;
    }

    static void Write(Utf8JsonWriter writer, JsonElement value)
    {
        switch (value.ValueKind)
        {
            case JsonValueKind.Object: WriteObject(writer, value); break;
            case JsonValueKind.Array: WriteArray(writer, value); break;
            default: value.WriteTo(writer); break;
        }
    }

    static void WriteObject(Utf8JsonWriter writer, JsonElement value)
    {
        writer.WriteStartObject();
        var properties = value.EnumerateObject().OrderBy(_ => _.Name, StringComparer.Ordinal).ToArray();
        if (properties.Select(_ => _.Name).Distinct(StringComparer.Ordinal).Count() != properties.Length) throw new InvalidMaterializationExecution();
        foreach (var property in properties)
        {
            writer.WritePropertyName(property.Name);
            Write(writer, property.Value);
        }
        writer.WriteEndObject();
    }

    static void WriteArray(Utf8JsonWriter writer, JsonElement value)
    {
        writer.WriteStartArray();
        foreach (var item in value.EnumerateArray()) Write(writer, item);
        writer.WriteEndArray();
    }

    JsonNode CaptureModel(ReadModelDefinition readModel)
    {
        var schemas = new JsonObject();
        foreach (var (generation, schema) in readModel.Schemas)
        {
            schemas[generation.Value.ToString(CultureInfo.InvariantCulture)] = JsonNode.Parse(schema.ToJson());
        }
        return JsonSerializer.SerializeToNode(new { readModel.Identifier, readModel.ContainerName, readModel.DisplayName, readModel.Owner, readModel.Source, readModel.ObserverType, readModel.ObserverIdentifier, readModel.Sink, Schemas = schemas, readModel.Indexes }, _options)!;
    }

    JsonNode[] CaptureEventSchemas(IReadOnlyCollection<EventTypeSchema> eventSchemas) => eventSchemas
        .Select(schema => JsonSerializer.SerializeToNode(new { schema.Type, schema.Owner, schema.Source, Schema = JsonNode.Parse(schema.Schema.ToJson()), schema.Visibility, schema.Origin }, _options)!)
        .OrderBy(schema => Canonicalize(schema.ToJsonString()), StringComparer.Ordinal)
        .ToArray();

    sealed class ExactEventDictionary<TDefinition> : DictionaryJsonConverter<EventType, TDefinition>
    {
        protected override EventType GetKeyFromString(string key) => EventType.Parse(key);

        protected override string GetKeyString(EventType key) => $"{key.Id.Value}+{key.Generation.Value}+{key.Tombstone}";
    }
}
