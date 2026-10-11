// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionFactory.when_generation_is_pinned;

public class and_a_from_and_join_pin_different_generations : given.a_projection_with_generations
{
    Exception _error;

    void Establish() => _definition = _definition with
    {
        From = new Dictionary<EventType, FromDefinition> { [new(_first.Id, 2)] = _definition.From[_second] },
        Join = new Dictionary<EventType, JoinDefinition> { [_first] = new("joinId", new Dictionary<PropertyPath, string>(), PropertyExpression.NotSet) }
    };

    async Task Because() => _error = await Catch.Exception(() => _factory.Create("store", "namespace", _definition, _readModel, _schemas));

    [Fact] void should_reject_ambiguous_pins() => _error.ShouldBeOfExactType<ObserverPinsMultipleGenerationsOfEventType>();
}
