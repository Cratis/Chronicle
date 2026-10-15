// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Properties;
using Cratis.Monads;

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionFactory.when_generation_is_pinned;

public class and_a_stored_join_is_at_another_generation : given.a_projection_with_generations
{
    IDictionary<string, object?> _joined;

    void Establish()
    {
        _definition = _definition with
        {
            From = new Dictionary<EventType, FromDefinition> { [_first] = new(new Dictionary<PropertyPath, string> { [(PropertyPath)"joinId"] = "parent" }, "key", null) },
            Join = new Dictionary<EventType, JoinDefinition> { [_second] = new("joinId", new Dictionary<PropertyPath, string> { [(PropertyPath)"newName"] = "name" }, "key") }
        };
        var stored = Event(new(_second.Id, 1), "protected-original");
        _sequence.TryGetLastEventBefore(_second.Id, "parent", Arg.Any<EventSequenceNumber>()).Returns(new Option<AppendedEvent>(stored));
        _release.Release(Arg.Any<EventStoreName>(), Arg.Is<IEnumerable<EventType>>(pins => pins.Single() == _second), Arg.Any<IDictionary<EventType, EventTypeSchema>>(), Arg.Any<IEnumerable<AppendedEvent>>()).Returns([Event(_second, "released-migrated")]);
    }

    async Task Because()
    {
        _projection = await _factory.Create("store", "namespace", _definition, _readModel, _schemas);
        var incoming = Event(_first, "original");
        var changes = new Changeset<AppendedEvent, ExpandoObject>(new ObjectComparer(), incoming, _state);
        _projection.OnNext(new(new("root", ArrayIndexers.NoIndexers), incoming, changes, ProjectionOperationType.From, false));
        _joined = (IDictionary<string, object?>)changes.Changes.OfType<ResolvedJoin>().Single().Changes.OfType<PropertiesChanged<ExpandoObject>>().Last().State;
    }

    [Fact] void should_project_the_released_join_generation() => _joined["newName"].ShouldEqual("released-migrated");
}
