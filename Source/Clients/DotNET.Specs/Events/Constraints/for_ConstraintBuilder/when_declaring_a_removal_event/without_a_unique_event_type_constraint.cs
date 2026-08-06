// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.Constraints.for_ConstraintBuilder.when_declaring_a_removal_event;

/// <summary>
/// The removal event releases the unique event type constraint it follows, so with nothing declared there is no
/// constraint for it to belong to. Saying so out loud beats quietly recording an event type nothing reads — the
/// author would get a constraint that never releases and no indication of why.
/// </summary>
public class without_a_unique_event_type_constraint : given.a_constraint_builder_with_owner
{
    Exception _error;

    void Because() => _error = Catch.Exception(() => _constraintBuilder.RemovedWith<LoanReturned>());

    [Fact] void should_say_there_is_no_constraint_to_release() => _error.ShouldBeOfExactType<NoUniqueEventTypeConstraintToRemove>();

    record LoanReturned();
}
