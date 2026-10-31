// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionFactory.when_generation_is_pinned;

public class and_removal_names_another_generation : given.a_projection_with_generations
{
    bool _removedByFirst;
    bool _removedBySecond;

    void Establish()
    {
        _definition = _definition with { From = _definition.From.Where(kvp => kvp.Key == _first).ToDictionary(), RemovedWith = new Dictionary<Concepts.Events.EventType, RemovedWithDefinition> { [_second] = new("key", null) } };
    }

    async Task Because()
    {
        _projection = await _factory.Create("store", "namespace", _definition, _readModel, _schemas);
        _removedByFirst = RemovedBy(Event(_first, "original"));
        _removedBySecond = RemovedBy(Event(_second, "migrated"));
    }

    [Fact] void should_not_remove_for_the_from_generation() => _removedByFirst.ShouldBeFalse();
    [Fact] void should_remove_for_the_removal_generation() => _removedBySecond.ShouldBeTrue();

    bool RemovedBy(Concepts.Events.AppendedEvent incoming)
    {
        var changeset = new Changeset<Concepts.Events.AppendedEvent, System.Dynamic.ExpandoObject>(new ObjectComparer(), incoming, _state);
        _projection.OnNext(new(new("root", ArrayIndexers.NoIndexers), incoming, changeset, ProjectionOperationType.From, false));
        return changeset.HasBeenRemoved();
    }
}
