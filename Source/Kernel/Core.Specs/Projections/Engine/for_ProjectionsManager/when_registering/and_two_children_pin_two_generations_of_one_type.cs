// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionsManager.when_registering;

public class and_two_children_pin_two_generations_of_one_type : given.a_projections_manager
{
    ProjectionRegistrationError _error;

    void Establish() => _firstDefinition = _firstDefinition with
    {
        GenerationDelivery = EventGenerationDelivery.Pinned,
        Children = new Dictionary<PropertyPath, ChildrenDefinition>
        {
            [(PropertyPath)"items"] = Child(new("PersonRegistered", 1)),
            [(PropertyPath)"otherItems"] = Child(new("PersonRegistered", 2))
        }
    };

    async Task Because()
    {
        var result = await _manager.Register(_eventStore, [_firstDefinition], [_firstReadModelDefinition], [_namespace]);
        result.TryGetError(out _error);
    }

    [Fact] void should_reject_pins_across_children() => _error.Failures[_firstDefinition.Identifier].GetBaseException().ShouldBeOfExactType<ObserverPinsMultipleGenerationsOfEventType>();
    [Fact] void should_not_register_the_definition() => _manager.TryGet(_eventStore, _namespace, _firstDefinition.Identifier, out _).ShouldBeFalse();

    ChildrenDefinition Child(EventType type) => new("id", new Dictionary<EventType, FromDefinition> { [type] = new(new Dictionary<PropertyPath, string>(), PropertyExpression.NotSet, null) }, new Dictionary<EventType, JoinDefinition>(), new Dictionary<PropertyPath, ChildrenDefinition>(), _firstDefinition.FromEvery, _firstDefinition.RemovedWith, _firstDefinition.RemovedWithJoin);
}
