// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Projections.for_ProjectionDefinitionConverters;

public class when_round_tripping_pinned_delivery : Specification
{
    ProjectionDefinition _restored;
    readonly EventType _pin = new("PersonRegistered", 2);
    readonly EventType _first = new("PersonRegistered", 1);
    int _legacyClauseCount;

    void Because()
    {
        var pinned = new ProjectionDefinition(ProjectionOwner.Server, EventSequenceId.Log, "projection", "model", true, true, new(), new Dictionary<EventType, FromDefinition> { [_pin] = new(new Dictionary<PropertyPath, string>(), PropertyExpression.NotSet, null) }, new Dictionary<EventType, JoinDefinition>(), new Dictionary<PropertyPath, ChildrenDefinition>(), [], new(new Dictionary<PropertyPath, string>(), false), new Dictionary<EventType, RemovedWithDefinition>(), new Dictionary<EventType, RemovedWithJoinDefinition>()) { GenerationDelivery = EventGenerationDelivery.Pinned };
        var definition = pinned with
        {
            From = new Dictionary<EventType, FromDefinition> { [_pin] = pinned.From[_pin], [_first] = pinned.From[_pin] },
            Children = new Dictionary<PropertyPath, ChildrenDefinition>
            {
                ["items"] = new("id", new Dictionary<EventType, FromDefinition> { [_pin] = pinned.From[_pin] }, new Dictionary<EventType, JoinDefinition>(), new Dictionary<PropertyPath, ChildrenDefinition>(), pinned.FromEvery, pinned.RemovedWith, pinned.RemovedWithJoin)
            }
        };
        var sql = definition.ToSql();
        _legacyClauseCount = JsonNode.Parse(sql.Definitions[ProjectionGeneration.First])!["From"]!.AsObject().Count;
        _restored = sql.ToKernel();
    }

    [Fact] void should_preserve_the_delivery_policy() => _restored.GenerationDelivery.ShouldEqual(EventGenerationDelivery.Pinned);
    [Fact] void should_preserve_both_clause_generations() => _restored.From.Keys.ShouldContainOnly(_pin, _first);
    [Fact] void should_preserve_the_child_generation() => _restored.Children["items"].From.Keys.Single().ShouldEqual(_pin);
    [Fact] void should_keep_the_legacy_representation_readable() => _legacyClauseCount.ShouldEqual(1);
}
