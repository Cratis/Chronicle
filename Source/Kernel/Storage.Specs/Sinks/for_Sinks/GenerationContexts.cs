// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Projections.Generations;
using Cratis.Chronicle.Concepts.Projections.Generations.Bindings;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;
using Cratis.Json;

namespace Cratis.Chronicle.Storage.Sinks.for_Sinks;

public static class GenerationContexts
{
    static readonly JsonSerializerOptions _options = new() { Converters = { new ConceptAsJsonConverterFactory(), new PropertyPathJsonConverter() } };

    public static MaterializationKey Key(SinkTypeId type) => new(new(Guid.Parse("015727e4-76b7-4713-b690-12b1188850a0")), "namespace", "projection", type, SinkConfigurationId.None);

    public static ReadModelDefinition Model(SinkTypeId type, uint schemaGeneration = 1) => new(
        "model",
        "models",
        "model",
        ReadModelOwner.Client,
        ReadModelSource.Code,
        ReadModelObserverType.Projection,
        "projection",
        new(SinkConfigurationId.None, type),
        new Dictionary<ReadModelGeneration, JsonSchema> { [new(schemaGeneration)] = JsonSchema.FromJson("{\"type\":\"object\",\"properties\":{\"name\":{\"type\":\"string\"}}}") },
        [new("name")]);

    public static MaterializationExecutionSpecification Capture(ReadModelDefinition model)
    {
        var schemas = model.Schemas.ToDictionary(pair => pair.Key.Value.ToString(System.Globalization.CultureInfo.InvariantCulture), pair => JsonSerializer.Deserialize<JsonElement>(pair.Value.ToJson()));
        var snapshot = new
        {
            ReadModel = new { model.Identifier, model.ContainerName, model.DisplayName, model.Owner, model.Source, model.ObserverType, model.ObserverIdentifier, model.Sink, Schemas = schemas, model.Indexes }
        };
        return MaterializationExecutionSpecification.Capture(EventSequenceId.Log, JsonSerializer.Serialize(snapshot, _options));
    }

    public static GenerationSinkContext Create(SinkTypeId type, uint generation = 1, uint schemaGeneration = 1, long epoch = 1)
    {
        var execution = Capture(Model(type, schemaGeneration));
        var binding = new GenerationObserverBinding(Key(type), MaterializationGenerationIdentity.For("projection", generation, "models"), execution.EventSequence, execution.Revision);
        return new(binding, execution, new("store", binding, new(Guid.NewGuid()), epoch));
    }
}
