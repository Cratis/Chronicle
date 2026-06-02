// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Projections.for_ProjectionReplayRecommendationEvaluator.given;

public class a_projection_replay_recommendation_evaluator : Specification
{
    protected static readonly EventType EventType1 = new("event-type-1", 1);
    protected static readonly EventType EventType2 = new("event-type-2", 1);
    protected readonly ObjectComparer ObjectComparer = new();

    protected static ProjectionDefinition CreateDefinition(
        IDictionary<EventType, FromDefinition>? from = null,
        IDictionary<EventType, JoinDefinition>? join = null,
        IEnumerable<FromDerivatives>? fromDerivatives = null) =>
        new(
            ProjectionOwner.Client,
            EventSequenceId.Log,
            "projection",
            "read-model",
            true,
            true,
            new System.Text.Json.Nodes.JsonObject(),
            from ?? new Dictionary<EventType, FromDefinition>(),
            join ?? new Dictionary<EventType, JoinDefinition>(),
            new Dictionary<PropertyPath, ChildrenDefinition>(),
            fromDerivatives ?? [],
            new FromEveryDefinition(new Dictionary<PropertyPath, string>(), false),
            new Dictionary<EventType, RemovedWithDefinition>(),
            new Dictionary<EventType, RemovedWithJoinDefinition>(),
            null,
            DateTimeOffset.UtcNow,
            null,
            AutoMap.Enabled);

    protected static ReadModelDefinition CreateReadModelDefinition(params JsonSchema[] schemas)
    {
        var schemaDictionary = new Dictionary<ReadModelGeneration, JsonSchema>();
        var generation = ReadModelGeneration.FirstValue;

        foreach (var schema in schemas)
        {
            schemaDictionary[generation] = schema;
            generation++;
        }

        return new(
            "read-model",
            "read-models",
            "Read Model",
            ReadModelOwner.Client,
            ReadModelSource.User,
            ReadModelObserverType.Projection,
            ReadModelObserverIdentifier.Unspecified,
            SinkDefinition.None,
            schemaDictionary,
            []);
    }
}
