// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Storage.InMemory.Projections.for_ProjectionDefinitionsStorage;

public class when_saving_pinned_delivery : Specification
{
    ProjectionDefinition _restored;
    readonly EventType _pin = new("PersonRegistered", 2);

    async Task Because()
    {
        var definition = new ProjectionDefinition(ProjectionOwner.Server, EventSequenceId.Log, "projection", "model", true, true, new(), new Dictionary<EventType, FromDefinition> { [_pin] = new(new Dictionary<PropertyPath, string>(), PropertyExpression.NotSet, null) }, new Dictionary<EventType, JoinDefinition>(), new Dictionary<PropertyPath, ChildrenDefinition>(), [], new(new Dictionary<PropertyPath, string>(), false), new Dictionary<EventType, RemovedWithDefinition>(), new Dictionary<EventType, RemovedWithJoinDefinition>()) { GenerationDelivery = EventGenerationDelivery.Pinned };
        var storage = new ProjectionDefinitionsStorage();
        await storage.Save(definition);
        _restored = await storage.Get(definition.Identifier);
    }

    [Fact] void should_preserve_the_delivery_policy() => _restored.GenerationDelivery.ShouldEqual(EventGenerationDelivery.Pinned);
    [Fact] void should_preserve_the_resolved_generation() => _restored.From.Keys.Single().ShouldEqual(_pin);
}
