// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionFactory.when_generation_is_pinned;

public class and_a_join_names_another_generation : given.a_projection_with_generations
{
    IDictionary<string, object?> _joined;
    bool _joinedByFirst;

    void Establish()
    {
        _definition = _definition with { From = _definition.From.Where(kvp => kvp.Key == _first).ToDictionary(), Join = new Dictionary<Concepts.Events.EventType, JoinDefinition> { [_second] = new("joinId", new Dictionary<PropertyPath, string> { [(PropertyPath)"newName"] = "name" }, "key") } };
    }

    async Task Because()
    {
        _projection = await _factory.Create("store", "namespace", _definition, _readModel, _schemas);
        var first = Event(_first, "original");
        var firstChanges = new Changeset<Concepts.Events.AppendedEvent, ExpandoObject>(new ObjectComparer(), first, _state);
        _projection.OnNext(new(new("root", ArrayIndexers.NoIndexers), first, firstChanges, ProjectionOperationType.From, false));
        _joinedByFirst = firstChanges.Changes.OfType<Joined>().Any();
        var second = Event(_second, "migrated");
        var changes = new Changeset<Concepts.Events.AppendedEvent, ExpandoObject>(new ObjectComparer(), second, firstChanges.CurrentState);
        _projection.OnNext(new(new("root", ArrayIndexers.NoIndexers), second, changes, ProjectionOperationType.Join, false));
        _joined = (IDictionary<string, object?>)changes.Changes.OfType<Joined>().Single().Changes.OfType<PropertiesChanged<ExpandoObject>>().Last().State;
    }

    [Fact] void should_not_join_the_first_generation() => _joinedByFirst.ShouldBeFalse();
    [Fact] void should_join_the_second_generation() => _joined["newName"].ShouldEqual("migrated");
}
