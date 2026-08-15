// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Concepts.EventSequences.Concurrency.for_ConcurrencyScope;

/// <summary>
/// A scope that narrows an append but carries no expected sequence number asks for a concurrency check and
/// gets none - the outcome is identical to never having asked, which is why it has to be recognizable rather
/// than quietly skipped along with the two scopes that legitimately opt out. A scope expecting
/// <see cref="EventSequenceNumber.BeforeFirst"/> is not one of these: it says what it expects, and the kernel
/// checks it.
/// </summary>
public class when_checking_whether_it_is_incomplete : Specification
{
    [Fact] void should_report_a_scope_with_metadata_but_no_sequence_number_as_incomplete() =>
        new ConcurrencyScope(EventSequenceNumber.Unavailable, false, null, null, new EventSourceType("Thing"), null).IsIncomplete.ShouldBeTrue();

    [Fact] void should_not_report_a_scope_with_a_sequence_number_as_incomplete() =>
        new ConcurrencyScope(42UL, false, null, null, new EventSourceType("Thing"), null).IsIncomplete.ShouldBeFalse();

    [Fact] void should_not_report_a_scope_expecting_no_matching_event_as_incomplete() =>
        new ConcurrencyScope(EventSequenceNumber.BeforeFirst, false, null, null, new EventSourceType("Thing"), null).IsIncomplete.ShouldBeFalse();

    [Fact] void should_not_report_the_not_set_scope_as_incomplete() => ConcurrencyScope.NotSet.IsIncomplete.ShouldBeFalse();
    [Fact] void should_not_report_the_none_scope_as_incomplete() => ConcurrencyScope.None.IsIncomplete.ShouldBeFalse();
}
