// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionsManager.when_registering;

public class and_a_from_and_join_pin_two_generations_of_one_type : given.a_projections_manager
{
    ProjectionRegistrationError _error;

    void Establish() => _firstDefinition = _firstDefinition with
    {
        GenerationDelivery = EventGenerationDelivery.Pinned,
        From = new Dictionary<EventType, FromDefinition> { [new("PersonRegistered", 1)] = new(new Dictionary<PropertyPath, string>(), PropertyExpression.NotSet, null) },
        Join = new Dictionary<EventType, JoinDefinition> { [new("PersonRegistered", 2)] = new("id", new Dictionary<PropertyPath, string>(), PropertyExpression.NotSet) }
    };

    async Task Because()
    {
        var result = await _manager.Register(_eventStore, [_firstDefinition], [_firstReadModelDefinition], []);
        result.TryGetError(out _error);
    }

    [Fact] void should_reject_the_definition_even_without_namespaces() => _error.Failures[_firstDefinition.Identifier].GetBaseException().ShouldBeOfExactType<ObserverPinsMultipleGenerationsOfEventType>();
    [Fact] void should_not_register_the_definition() => _manager.TryGet(_eventStore, _namespace, _firstDefinition.Identifier, out _).ShouldBeFalse();
}
