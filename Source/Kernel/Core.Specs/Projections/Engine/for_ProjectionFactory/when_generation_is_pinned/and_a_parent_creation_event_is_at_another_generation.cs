// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Projections.Engine.for_KeyResolvers;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionFactory.when_generation_is_pinned;

public class and_a_parent_creation_event_is_at_another_generation : given.a_projection_with_generations
{
    ResolvedKey _key;

    void Establish()
    {
        var childType = new EventType("ChildAdded", 2);
        _schemas = [.. _schemas, new(childType, EventTypeOwner.Client, EventTypeSource.Code, _schemas[1].Schema)];
        _definition = _definition with
        {
            From = _definition.From.Where(kvp => kvp.Key == _second).ToDictionary(),
            Children = new Dictionary<PropertyPath, ChildrenDefinition>
            {
                [(PropertyPath)"items"] = new("id", new Dictionary<EventType, FromDefinition> { [childType] = new(new Dictionary<PropertyPath, string>(), "key", "parent") }, new Dictionary<EventType, JoinDefinition>(), new Dictionary<PropertyPath, ChildrenDefinition>(), _definition.FromEvery, _definition.RemovedWith, _definition.RemovedWithJoin)
            }
        };
        var parent = Event(new(_second.Id, 1), "protected-parent");
        parent = parent with { Context = parent.Context with { EventSourceId = "parent", SequenceNumber = 1 } };
        _sequence.WithParentHistory("parent", parent);
        var released = Event(_second, "released-parent");
        released = released with { Context = parent.Context with { EventType = _second } };
        _release.Release(Arg.Any<EventStoreName>(), Arg.Is<IEnumerable<EventType>>(pins => pins.Single() == _second), Arg.Any<IDictionary<EventType, EventTypeSchema>>(), Arg.Any<IEnumerable<AppendedEvent>>()).Returns([released]);
    }

    async Task Because()
    {
        _projection = await _factory.Create("store", "namespace", _definition, _readModel, _schemas);
        var childType = new EventType("ChildAdded", 2);
        var child = Event(childType, "child");
        child = child with { Context = child.Context with { SequenceNumber = 2 } };
        _key = (ResolvedKey)await _projection.GetKeyResolverFor(childType)(_sequence, NullSink.Instance, child);
    }

    [Fact] void should_resolve_the_root_key_from_the_released_parent_generation() => _key.Key.Value.ShouldEqual("key-2");
}
