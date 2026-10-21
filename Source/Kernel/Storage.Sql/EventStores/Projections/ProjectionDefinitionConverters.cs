// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Concepts.Projections.Json;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Properties;
using Cratis.Json;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Projections;

/// <summary>
/// Converter methods for working with <see cref="ProjectionDefinition"/> converting to and from SQL representations.
/// </summary>
public static class ProjectionDefinitionConverters
{
    static readonly JsonSerializerOptions _serializerOptions = new()
    {
        Converters =
        {
            new ConceptAsJsonConverterFactory(),
            new KeyJsonConverter(),
            new PropertyPathJsonConverter(),
            new PropertyPathChildrenDefinitionDictionaryJsonConverter(),
            new PropertyExpressionDictionaryConverter(),
            new FromDefinitionsConverter(),
            new JoinDefinitionsConverter(),
            new RemovedWithDefinitionsConverter(),
            new RemovedWithJoinDefinitionsConverter()
        }
    };

    static readonly JsonSerializerOptions _pinnedSerializerOptions = CreatePinnedSerializerOptions();

    /// <summary>
    /// Convert to a <see cref="Projection">SQL</see> representation.
    /// </summary>
    /// <param name="definition"><see cref="ProjectionDefinition"/> to convert.</param>
    /// <returns>Converted <see cref="Projection"/>.</returns>
    public static Projection ToSql(this ProjectionDefinition definition) =>
        new()
        {
            Id = definition.Identifier,
            Owner = definition.Owner,
            ReadModelName = definition.ReadModel,
            ReadModelGeneration = ReadModelGeneration.First,
            Definitions = new Dictionary<uint, string>
            {
                { ProjectionGeneration.First, Serialize(definition) }
            }
        };

    /// <summary>
    /// Convert to <see cref="ProjectionDefinition"/> from <see cref="Projection"/>.
    /// </summary>
    /// <param name="projection"><see cref="Projection"/> to convert from.</param>
    /// <returns>Converted <see cref="ProjectionDefinition"/>.</returns>
    /// <exception cref="PinnedProjectionDefinitionNotStored">The pinned generation-exact definition is missing.</exception>
    public static ProjectionDefinition ToKernel(this Projection projection)
    {
        var json = JsonNode.Parse(projection.Definitions[ProjectionGeneration.First])!.AsObject();
        if (json["PinnedDefinition"] is { } pinned)
        {
            return pinned.Deserialize<ProjectionDefinition>(_pinnedSerializerOptions)!;
        }

        var definition = json.Deserialize<ProjectionDefinition>(_serializerOptions)!;
        if (definition.GenerationDelivery == EventGenerationDelivery.Pinned)
        {
            throw new PinnedProjectionDefinitionNotStored(definition.Identifier);
        }

        return definition;
    }

    static string Serialize(ProjectionDefinition definition)
    {
        if (definition.GenerationDelivery != EventGenerationDelivery.Pinned)
        {
            return JsonSerializer.Serialize(definition, _serializerOptions);
        }

        // Keep the existing JSON members readable by older silos. The additive member retains every
        // clause's exact generation, including multiple generations of one identifier at the same level.
        var json = JsonSerializer.SerializeToNode(ForLegacyReader(definition), _serializerOptions)!.AsObject();
        json["PinnedDefinition"] = JsonSerializer.SerializeToNode(definition, _pinnedSerializerOptions);

        return json.ToJsonString();
    }

    static ProjectionDefinition ForLegacyReader(ProjectionDefinition definition) => definition with
    {
        From = definition.From.DistinctBy(pair => pair.Key.Id).ToDictionary(),
        Join = definition.Join.DistinctBy(pair => pair.Key.Id).ToDictionary(),
        RemovedWith = definition.RemovedWith.DistinctBy(pair => pair.Key.Id).ToDictionary(),
        RemovedWithJoin = definition.RemovedWithJoin.DistinctBy(pair => pair.Key.Id).ToDictionary(),
        Children = definition.Children.ToDictionary(pair => pair.Key, pair => (ChildrenDefinition)ForLegacyReader(pair.Value)),
        Nested = definition.Nested?.ToDictionary(pair => pair.Key, pair => (ChildrenDefinition)ForLegacyReader(pair.Value))
    };

    static JsonSerializerOptions CreatePinnedSerializerOptions()
    {
        var options = new JsonSerializerOptions(_serializerOptions);
        options.Converters.Insert(0, new PinnedFromDefinitionsConverter());
        options.Converters.Insert(0, new PinnedJoinDefinitionsConverter());
        options.Converters.Insert(0, new PinnedRemovedWithDefinitionsConverter());
        options.Converters.Insert(0, new PinnedRemovedWithJoinDefinitionsConverter());

        return options;
    }

    sealed class PinnedFromDefinitionsConverter : DictionaryJsonConverter<EventType, FromDefinition>
    {
        /// <inheritdoc/>
        protected override EventType GetKeyFromString(string key) => EventType.Parse(key);

        /// <inheritdoc/>
        protected override string GetKeyString(EventType key) => key.ToString();
    }

    sealed class PinnedJoinDefinitionsConverter : DictionaryJsonConverter<EventType, JoinDefinition>
    {
        /// <inheritdoc/>
        protected override EventType GetKeyFromString(string key) => EventType.Parse(key);

        /// <inheritdoc/>
        protected override string GetKeyString(EventType key) => key.ToString();
    }

    sealed class PinnedRemovedWithDefinitionsConverter : DictionaryJsonConverter<EventType, RemovedWithDefinition>
    {
        /// <inheritdoc/>
        protected override EventType GetKeyFromString(string key) => EventType.Parse(key);

        /// <inheritdoc/>
        protected override string GetKeyString(EventType key) => key.ToString();
    }

    sealed class PinnedRemovedWithJoinDefinitionsConverter : DictionaryJsonConverter<EventType, RemovedWithJoinDefinition>
    {
        /// <inheritdoc/>
        protected override EventType GetKeyFromString(string key) => EventType.Parse(key);

        /// <inheritdoc/>
        protected override string GetKeyString(EventType key) => key.ToString();
    }
}
