// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Arc.Queries.ModelBound;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Grpc;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.ReadModelLookups;

/// <summary>
/// The result of reading a materialized instance by structured composite-key parts.
/// </summary>
/// <param name="Outcome">The outcome of the lookup.</param>
/// <param name="ReadModel">The released instance as JSON, or the literal null.</param>
/// <param name="LastHandledEventSequenceNumber">The materialized instance's watermark.</param>
/// <param name="Message">Validation details or the reason a lookup was refused.</param>
[ReadModel]
[BelongsTo(WellKnownServices.ReadModelLookups)]
public record ReadModelLookup(ReadModelLookupOutcome Outcome, string ReadModel, ulong LastHandledEventSequenceNumber, string Message)
{
    /// <summary>
    /// Reads a materialized instance without replay or immediate projection fallback.
    /// </summary>
    /// <param name="eventStore">The event store.</param>
    /// <param name="namespace">The event store namespace.</param>
    /// <param name="readModel">The registered read model identifier.</param>
    /// <param name="parts">Key parts by name, each encoded as a JSON literal.</param>
    /// <param name="grainFactory">The grain factory for registered definitions.</param>
    /// <param name="store">The materialized store.</param>
    /// <param name="typeFormats">The schema's type formats.</param>
    /// <param name="expandoObjectConverter">The JSON converter.</param>
    /// <param name="jsonSerializerOptions">The JSON serialization options.</param>
    /// <returns>The lookup outcome and released instance when found.</returns>
    /// <exception cref="ReadModelNotFound">The read model is not registered.</exception>
    public static async Task<ReadModelLookup> FindByCompositeKey(
        EventStoreName eventStore,
        EventStoreNamespaceName @namespace,
        string readModel,
        IDictionary<string, string> parts,
        IGrainFactory grainFactory,
        IMaterializedReadModelStore store,
        ITypeFormats typeFormats,
        IExpandoObjectConverter expandoObjectConverter,
        JsonSerializerOptions jsonSerializerOptions)
    {
        var identifier = new ReadModelIdentifier(readModel);
        var definition = await grainFactory.GetReadModel(identifier, eventStore).GetDefinition();
        if (definition?.Sink is null) throw new ReadModelNotFound(identifier);
        if (!store.IsMaterialized(definition)) return ReadModelLookupResults.Empty(ReadModelLookupOutcome.NotSupported, "Only materialized read models can be looked up by composite key.");
        var schema = definition.GetSchemaForLatestGeneration();
        var normalization = CompositeKeyNormalizer.Normalize(schema, parts.AsReadOnly(), typeFormats);
        if (!normalization.IsValid) return ReadModelLookupResults.Empty(ReadModelLookupOutcome.InvalidKey, normalization.Message);
        var result = await store.FindByCompositeKey(eventStore, @namespace, definition, new Key(normalization.Key, ArrayIndexers.NoIndexers));
        if (result.Outcome != CompositeKeyLookupOutcome.Found) return ReadModelLookupResults.From(result.Outcome);
        var instance = result.Instance!;
        var json = expandoObjectConverter.ToJsonObject(instance.WithoutInitializationState(schema), schema);
        return new(ReadModelLookupOutcome.Found, json.ToJsonString(jsonSerializerOptions), instance.GetLastHandledEventSequenceNumber().Value, string.Empty);
    }
}
