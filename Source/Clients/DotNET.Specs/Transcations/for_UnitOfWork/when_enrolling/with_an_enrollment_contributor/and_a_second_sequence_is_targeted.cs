// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;

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
    [Fact] void should_identify_the_required_event_source() => ((ConflictingEnrollmentScope)_error).EventSourceId.ShouldEqual(_source);
    [Fact] void should_identify_the_attempted_sequence() => ((ConflictingEnrollmentScope)_error).EventSequenceId.ShouldEqual(EventSequenceId.Outbox);
    [Fact] void should_carry_the_absent_effective_scope() => ((ConflictingEnrollmentScope)_error).Effective.ShouldEqual(ConcurrencyScope.NotSet);
    [Fact] void should_carry_the_recorded_requirement() => ((ConflictingEnrollmentScope)_error).Required.ShouldEqual(_required);
    [Fact] void should_redact_the_event_source() => _error.Message.ShouldNotContain(_source.Value);
    [Fact] void should_redact_the_attempted_sequence() => _error.Message.ShouldNotContain(EventSequenceId.Outbox.Value);
    [Fact] void should_redact_the_required_scope() => _error.Message.ShouldNotContain(_required.ToString());
    [Fact] void should_redact_the_effective_scope() => _error.Message.ShouldNotContain(ConcurrencyScope.NotSet.ToString());
    [Fact] void should_preserve_the_first_event_only() => _unitOfWork.GetEvents().ShouldContainOnly(_event);
}
