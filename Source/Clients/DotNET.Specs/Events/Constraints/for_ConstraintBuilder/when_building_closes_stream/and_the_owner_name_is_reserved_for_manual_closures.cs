// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.Constraints.for_ConstraintBuilder.when_building_closes_stream;

public class and_the_owner_name_is_reserved_for_manual_closures : given.a_constraint_builder_with_owner
{
    Exception _error;

    void Establish() => _eventTypes.GetEventTypeFor(typeof(Closed)).Returns(new EventType(nameof(Closed), EventTypeGeneration.First));

    void Because() => _error = Catch.Exception(() => _constraintBuilder.ClosesStreamOn<Closed>(name: string.Empty));

    [Fact] void should_not_allow_the_manual_owner_name() => _error.ShouldBeOfExactType<MissingNameForClosesStreamConstraint>();

    record Closed();
}
