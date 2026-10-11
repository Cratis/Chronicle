// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Reducers.for_Reducers.when_registering;

public class and_two_generations_of_one_type_are_pinned : given.all_dependencies
{
    Exception _error;
    protected override EventGenerationDelivery GenerationDelivery => EventGenerationDelivery.Pinned;
    void Establish()
    {
        var handler = Substitute.For<IReducerHandler>();
        handler.ReducerType.Returns(typeof(object));
        handler.ReadModelType.Returns(typeof(object));
        handler.IsActive.Returns(true);
        handler.EventTypes.Returns([new EventType("person-registered", 1), new EventType("person-registered", 2)]);
        _handlersByModelType[typeof(object)] = handler;
    }
    async Task Because() => _error = await Catch.Exception(_reducers.Register);
    [Fact] void should_reject_ambiguous_pins() => _error.ShouldBeOfExactType<MultipleGenerationsOfEventTypeInObserver>();
}
