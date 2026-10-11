// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Reactors.for_Reactors.when_registering;

public class and_two_generations_of_one_type_are_pinned : given.all_dependencies
{
    Exception _error;
    protected override EventGenerationDelivery GenerationDelivery => EventGenerationDelivery.Pinned;
    async Task Because() => _error = await Catch.Exception(() => _reactors.Register("ambiguous", builder => builder.WithEventType(new EventType("person-registered", 1)).WithEventType(new EventType("person-registered", 2)), (_, _) => Task.CompletedTask));
    [Fact] void should_reject_ambiguous_pins() => _error.ShouldBeOfExactType<MultipleGenerationsOfEventTypeInObserver>();
}
