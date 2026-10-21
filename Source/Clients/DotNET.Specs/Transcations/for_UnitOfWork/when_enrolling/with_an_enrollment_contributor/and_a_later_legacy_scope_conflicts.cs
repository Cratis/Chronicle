// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.when_enrolling.with_an_enrollment_contributor;

public class and_a_later_legacy_scope_conflicts : given.a_contributed_unit
{
    Exception _error;

    void Establish()
    {
        Stage();
        _registration.Dispose();
    }

    void Because() => _error = Catch.Exception(() => Stage(ConcurrencyScope.None));

    [Fact] void should_reject_the_conflict_with_the_recorded_guard() => _error.ShouldBeOfExactType<ConflictingEnrollmentScope>();
    [Fact] void should_identify_the_event_source() => ((ConflictingEnrollmentScope)_error).EventSourceId.ShouldEqual(_source);
    [Fact] void should_identify_the_attempted_sequence() => ((ConflictingEnrollmentScope)_error).EventSequenceId.ShouldEqual(EventSequenceId.Log);
    [Fact] void should_carry_the_effective_scope() => ((ConflictingEnrollmentScope)_error).Effective.ShouldEqual(ConcurrencyScope.None);
    [Fact] void should_carry_the_recorded_requirement() => ((ConflictingEnrollmentScope)_error).Required.ShouldEqual(_required);
    [Fact] void should_describe_only_the_differing_dimensions() => _error.Message.ShouldEqual("Enrollment conflicts with its required scope or event sequence. Differing dimensions: sequence number, event source id.");
    [Fact] void should_redact_the_event_source() => _error.Message.ShouldNotContain(_source.Value);
    [Fact] void should_redact_the_required_scope() => _error.Message.ShouldNotContain(_required.ToString());
    [Fact] void should_redact_the_effective_scope() => _error.Message.ShouldNotContain(ConcurrencyScope.None.ToString());
    [Fact] void should_preserve_only_the_previous_event() => _unitOfWork.GetEvents().ShouldContainOnly(_event);
}
