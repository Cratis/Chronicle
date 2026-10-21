// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionFactory.when_generation_is_pinned;

public class and_two_child_from_clauses_pin_different_generations : given.a_projection_with_generations
{
    Exception _error;

    void Establish() => _definition = _definition with
    {
        From = new Dictionary<EventType, FromDefinition>(),
        Children = new Dictionary<PropertyPath, ChildrenDefinition>
        {
            [(PropertyPath)"items"] = Child(_first),
            [(PropertyPath)"otherItems"] = Child(new(_first.Id, 2))
        }
    };

    async Task Because() => _error = await Catch.Exception(() => _factory.Create("store", "namespace", _definition, _readModel, _schemas));

    [Fact] void should_reject_ambiguous_pins_across_children() => _error.ShouldBeOfExactType<ObserverPinsMultipleGenerationsOfEventType>();

    ChildrenDefinition Child(EventType type) => new("id", new Dictionary<EventType, FromDefinition> { [type] = new(new Dictionary<PropertyPath, string>(), PropertyExpression.NotSet, null) }, new Dictionary<EventType, JoinDefinition>(), new Dictionary<PropertyPath, ChildrenDefinition>(), _definition.FromEvery, _definition.RemovedWith, _definition.RemovedWithJoin);
}
