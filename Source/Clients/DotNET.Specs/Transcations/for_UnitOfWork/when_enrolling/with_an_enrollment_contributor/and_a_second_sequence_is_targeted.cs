// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.when_enrolling.with_an_enrollment_contributor;

public class and_a_second_sequence_is_targeted : given.a_contributed_unit
{
    Exception _error;

    void Establish()
    {
        Stage();
        _registration.Dispose();
    }

    void Because() => _error = Catch.Exception(() => _unitOfWork.AddEvent(EventSequenceId.Outbox, EventSourceId.New(), new object(), Causation.Unknown()));

    [Fact] void should_refuse_even_an_unguarded_source_on_another_sequence() => _error.ShouldBeOfExactType<ConflictingEnrollmentScope>();
    [Fact] void should_preserve_the_first_event_only() => _unitOfWork.GetEvents().ShouldContainOnly(_event);
}
