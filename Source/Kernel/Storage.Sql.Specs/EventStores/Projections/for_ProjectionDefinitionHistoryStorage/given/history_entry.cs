// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Storage.Projections;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Projections.for_ProjectionDefinitionHistoryStorage.given;

public static class history_entry
{
    public static ProjectionDefinitionHistoryEntry Create()
    {
        var first = new EventType("event", 1);
        var second = new EventType("event", 2);
        var definition = new ProjectionDefinition(ProjectionOwner.Client, EventSequenceId.Log, "projection", "model", true, true, new(), new Dictionary<EventType, FromDefinition> { [first] = new(new Dictionary<PropertyPath, string>(), PropertyExpression.NotSet, null), [second] = new(new Dictionary<PropertyPath, string>(), PropertyExpression.NotSet, null) }, new Dictionary<EventType, JoinDefinition>(), new Dictionary<PropertyPath, ChildrenDefinition>(), [], new(new Dictionary<PropertyPath, string>(), false), new Dictionary<EventType, RemovedWithDefinition>(), new Dictionary<EventType, RemovedWithJoinDefinition>()) { Version = "1.0.0", LastUpdated = DateTimeOffset.FromUnixTimeSeconds(90), GenerationDelivery = EventGenerationDelivery.Pinned };
        return new(definition.Identifier, new string('A', 64), "definition-content-sha256/v1", definition.Version, DateTimeOffset.FromUnixTimeSeconds(100), definition);
    }
}
