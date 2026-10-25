// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Projections.Generations;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Generations.Bindings;
using Cratis.Json;

namespace Cratis.Chronicle.Storage.Sinks;

/// <summary>
/// Addresses immutable generation execution and its fixed, explicitly authorized writer grant.
/// </summary>
/// <param name="Binding">The verified observer binding, never inferred from an identifier suffix.</param>
/// <param name="Execution">The persisted execution snapshot.</param>
/// <param name="Grant">The owner's fixed grant shared by execution participants.</param>
public sealed record GenerationSinkContext(GenerationObserverBinding Binding, MaterializationExecutionSpecification Execution, GenerationWriterGrant Grant)
{
    static readonly JsonSerializerOptions _options = CreateOptions();

    /// <summary>
    /// Verifies routing and returns a detached model from the snapshot, including its immutable index set.
    /// </summary>
    /// <param name="store">The requested store.</param>
    /// <param name="namespace">The requested namespace.</param>
    /// <returns>A detached definition addressing the exclusively claimed destination.</returns>
    /// <exception cref="InvalidMaterializationExecution">The context does not describe this execution.</exception>
    public ReadModelDefinition GetReadModel(EventStoreName store, EventStoreNamespaceName @namespace)
    {
        Execution.EnsureValid();
        if (Grant.Store != store || Binding.Key.Namespace != @namespace || Grant.Binding != Binding) throw new InvalidMaterializationExecution();
        if (Grant.Epoch <= 0 || Binding.Key.Projection != Binding.Identity.ProjectionId) throw new InvalidMaterializationExecution();
        if (Binding.Revision != Execution.Revision || Binding.EventSequence != Execution.EventSequence) throw new InvalidMaterializationExecution();
        using var document = JsonDocument.Parse(Execution.CanonicalContent);
        var model = document.RootElement.EnumerateObject().Single(_ => string.Equals(_.Name, "ReadModel", StringComparison.OrdinalIgnoreCase)).Value.Deserialize<ReadModelDefinition>(_options)
            ?? throw new InvalidMaterializationExecution();
        if (model.Sink.Type != Binding.Key.SinkType || model.Sink.Configuration != Binding.Key.SinkConfiguration) throw new InvalidMaterializationExecution();
        return model with { ContainerName = Binding.Identity.Destination };
    }

    static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        options.Converters.Add(new ConceptAsJsonConverterFactory());
        options.Converters.Add(new PropertyPathJsonConverter());
        options.Converters.Add(new JsonStringEnumConverter());
        options.Converters.Add(new SchemaDictionaryConverter());
        return options;
    }

    sealed class SchemaDictionaryConverter : JsonConverter<IDictionary<ReadModelGeneration, JsonSchema>>
    {
        public override IDictionary<ReadModelGeneration, JsonSchema> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using var document = JsonDocument.ParseValue(ref reader);
            return document.RootElement.EnumerateObject().ToDictionary(
                property => new ReadModelGeneration(uint.Parse(property.Name, CultureInfo.InvariantCulture)),
                property => JsonSchema.FromJson(property.Value.GetRawText()));
        }

        public override void Write(Utf8JsonWriter writer, IDictionary<ReadModelGeneration, JsonSchema> value, JsonSerializerOptions options) => throw new NotSupportedException();
    }
}
