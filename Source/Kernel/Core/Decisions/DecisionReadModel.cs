// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Arc.Queries.ModelBound;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Grpc;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage;
using static Cratis.Chronicle.Decisions.DecisionReadRefusals;

namespace Cratis.Chronicle.Decisions;

/// <summary>
/// An event-log-derived instance and exactly the event types and watermark needed to guard a decision.
/// </summary>
/// <param name="Instance">JSON instance, or "null" when absent.</param>
/// <param name="SequenceNumber">Last matching event folded, or Unavailable when there were none.</param>
/// <param name="EventTypes">Complete projected event types, including removal events.</param>
/// <param name="Refusal">Why this read cannot be guarded, or None.</param>
[ReadModel]
[BelongsTo(WellKnownServices.DecisionReadModels)]
public record DecisionReadModel(string Instance, ulong SequenceNumber, IEnumerable<string> EventTypes, DecisionReadRefusal Refusal)
{
    /// <summary>
    /// Computes the read model and its concurrency watermark from the event log, never from a sink.
    /// </summary>
    /// <param name="eventStore">The event store.</param>
    /// <param name="namespace">The namespace.</param>
    /// <param name="readModelIdentifier">The registered read model identifier.</param>
    /// <param name="key">The event source / read model key.</param>
    /// <param name="grainFactory">Grain factory.</param>
    /// <param name="compliance">Read model compliance release.</param>
    /// <param name="jsonSerializerOptions">Serialization options.</param>
    /// <returns>An exact read or a typed refusal.</returns>
    public static async Task<DecisionReadModel> GetInstanceForDecision(
        EventStoreName eventStore,
        EventStoreNamespaceName @namespace,
        ReadModelIdentifier readModelIdentifier,
        ReadModelKey key,
        IGrainFactory grainFactory,
        IReadModelsCompliance compliance,
        JsonSerializerOptions jsonSerializerOptions)
    {
        if (string.IsNullOrWhiteSpace(key) || key == ReadModelKey.Unspecified.Value)
        {
            return Refuse(DecisionReadRefusal.UnspecifiedKey);
        }

        var readModel = grainFactory.GetReadModel(readModelIdentifier, eventStore);
        var definition = await readModel.GetDefinition();
        if (definition?.Identifier is null || string.IsNullOrWhiteSpace(definition.ObserverIdentifier?.Value))
        {
            return Refuse(DecisionReadRefusal.UnknownDefinition);
        }

        if (definition.ObserverType == ReadModelObserverType.Reducer)
        {
            return Refuse(DecisionReadRefusal.Reducer);
        }

        if (definition.ObserverType != ReadModelObserverType.Projection)
        {
            return Refuse(DecisionReadRefusal.UnknownDefinition);
        }

        var projectionId = (ProjectionId)definition.ObserverIdentifier.Value;
        var projection = grainFactory.GetGrain<IProjection>(new ProjectionKey(projectionId, eventStore));
        var projectionDefinition = await projection.GetDefinition();
        if (projectionDefinition?.ReadModel is null || projectionDefinition.ReadModel != definition.Identifier)
        {
            return Refuse(DecisionReadRefusal.UnknownDefinition);
        }

        if (projectionDefinition.EventSequenceId != EventSequenceId.Log)
        {
            return Refuse(DecisionReadRefusal.NotEventLog);
        }

        var shape = await projection.GetDecisionProjectionShape(@namespace);
        if (shape.HasJoins) return Refuse(DecisionReadRefusal.Join);
        if (shape.HasChildren) return Refuse(DecisionReadRefusal.Hierarchy);
        if (shape.SubscribesToAllEvents) return Refuse(DecisionReadRefusal.OpenEndedEventTypes);
        if (!shape.IsEventSourceKeyed || shape.EventTypes.Count == 0) return Refuse(DecisionReadRefusal.NotEventSourceKeyed);

        // An isolated session grain is initialized with the admitted definition and discarded after the
        // call. It cannot serve cached state folded under a previous definition, nor session-only events.
        var projectionKey = new ImmediateProjectionKey(projectionId, eventStore, @namespace, EventSequenceId.Log, key, (ProjectionSessionId)Guid.NewGuid());
        var immediate = grainFactory.GetGrain<IImmediateProjection>(projectionKey);
        try
        {
            await immediate.InitializeForDecision(projectionDefinition, shape.EventTypes);
            var result = await immediate.GetModelInstance();
            var currentDefinition = await projection.GetDefinition();
            var currentShape = await projection.GetDecisionProjectionShape(@namespace);
            if (currentDefinition is null ||
                currentDefinition.ReadModel != projectionDefinition.ReadModel ||
                !string.Equals(
                    CanonicalDefinition(projectionDefinition, jsonSerializerOptions),
                    CanonicalDefinition(currentDefinition, jsonSerializerOptions),
                    StringComparison.Ordinal) ||
                !currentShape.EventTypes.SequenceEqual(shape.EventTypes) ||
                !currentShape.IsEventSourceKeyed || currentShape.HasJoins || currentShape.HasChildren || currentShape.SubscribesToAllEvents)
            {
                return Refuse(DecisionReadRefusal.DefinitionChanged);
            }

            if (!result.HasReadModel)
            {
                return new("null", result.LastHandledEventSequenceNumber, shape.EventTypes.Select(_ => _.ToString()).ToArray(), DecisionReadRefusal.None);
            }

            var model = result.ReadModel;
            var schema = definition.GetSchemaForLatestGeneration();
            if (schema.HasSchemaMetadata())
            {
                var stamped = (JsonObject)model.DeepClone();
                stamped[WellKnownProperties.Subject] = key.Value;
                model = await compliance.ReleaseJson(eventStore, @namespace, schema, stamped);
                model.Remove(WellKnownProperties.Subject);
            }

            return new(model.ToJsonString(jsonSerializerOptions), result.LastHandledEventSequenceNumber, shape.EventTypes.Select(_ => _.ToString()).ToArray(), DecisionReadRefusal.None);
        }
        finally
        {
            await immediate.Dehydrate();
        }
    }

    /// <summary>
    /// Sorts keyed definition dictionaries recursively; array order remains significant.
    /// </summary>
    /// <param name="definition">The projection definition.</param>
    /// <param name="options">Kernel JSON serialization options.</param>
    /// <returns>A stable serialized definition.</returns>
    static string CanonicalDefinition(Concepts.Projections.Definitions.ProjectionDefinition definition, JsonSerializerOptions options)
    {
        var node = JsonSerializer.SerializeToNode(definition with { LastUpdated = null }, options);

        // The projection JSON dictionary converters use event type IDs as property names; retain
        // generations explicitly so a generation-only definition edit cannot evade this check.
        var eventTypeKeys = new[]
        {
            definition.From.Keys.Select(type => $"from:{type}"),
            definition.Join.Keys.Select(type => $"join:{type}"),
            definition.RemovedWith.Keys.Select(type => $"removed:{type}"),
            definition.RemovedWithJoin.Keys.Select(type => $"removedJoin:{type}")
        }.SelectMany(types => types).Order(StringComparer.Ordinal);
        return Canonicalize(node)!.ToJsonString() + "|" + string.Join('|', eventTypeKeys);
    }

    static JsonNode? Canonicalize(JsonNode? node) => node switch
    {
        JsonObject obj => new JsonObject(obj.OrderBy(pair => pair.Key, StringComparer.Ordinal)
            .Select(pair => KeyValuePair.Create(pair.Key, pair.Value is null ? null : Canonicalize(pair.Value)))),
        JsonArray array => new JsonArray(array.Select(item => item is null ? null : Canonicalize(item)).ToArray()),
        null => null,
        _ => node.DeepClone()
    };
}
