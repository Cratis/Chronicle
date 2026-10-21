// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Chronicle.Transactions.for_ConflictingEnrollmentScope.when_describing_a_conflict;

public class with_equivalent_scope_filters : Specification
{
    ConcurrencyScope _effective;
    ConcurrencyScope _required;
    ConflictingEnrollmentScope _conflict;

    void Establish()
    {
        EventType first = new("first-sensitive-event", EventTypeGeneration.First);
        EventType second = new("second-sensitive-event", EventTypeGeneration.First);
        _effective = new(13579, "sensitive-source", EventStreamType.All, EventStreamId.Default, EventSourceType.Unspecified, [first, second, first]);
        _required = new(13579, "sensitive-source", EventTypes: [second, first]);
    }

    void Because() => _conflict = new("sensitive-source", _effective, _required);

    [Fact] void should_not_report_equivalent_filters_as_different() => _conflict.Message.ShouldEqual("Enrollment conflicts with its required scope or event sequence.");
    [Fact] void should_preserve_the_original_constructor_without_sequence_information() => _conflict.EventSequenceId.ShouldBeNull();
    [Fact] void should_preserve_the_effective_scope() => _conflict.Effective.ShouldBeSame(_effective);
    [Fact] void should_preserve_the_required_scope() => _conflict.Required.ShouldBeSame(_required);
}
