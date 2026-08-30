// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.EventSequences.Concurrency.for_ConcurrencyScopeConverters.when_converting_to_sequences_contract;

/// <summary>
/// <para>
/// This is the half of the wire format that decides what a version mismatch does, so it is pinned by value rather
/// than by round trip. The expectation travels in its own field; the number field carries
/// <see cref="EventSequenceNumber.Unavailable"/>.
/// </para>
/// <para>
/// A kernel that predates the field reads only the number. <see cref="EventSequenceNumber.Unavailable"/> is the
/// value it already refuses to validate, so it skips the check and logs the warning it always has - the older
/// behavior, honestly reported. Sending <see cref="EventSequenceNumber.BeforeFirst"/> in the number field instead
/// would make that same kernel treat it as an ordinary expected sequence number near the top of the range, compare
/// a real tail against it, and pass - a concurrency guard that reports success without ever running, which is worse
/// than the skip this change set out to remove.
/// </para>
/// </summary>
public class and_the_scope_expects_no_matching_event : Specification
{
    ConcurrencyScope _scope;
    Contracts.Sequences.ConcurrencyScope _result;

    void Establish() => _scope = new ConcurrencyScope(
        EventSequenceNumber.BeforeFirst,
        new EventSourceId("some-event-source-id"),
        EventSourceType: new EventSourceType("Customer"));

    void Because() => _result = _scope.ToSequencesContract();

    [Fact] void should_declare_the_expectation_in_its_own_field() => _result.ExpectsNoMatchingEvent.ShouldBeTrue();
    [Fact] void should_send_the_number_an_older_kernel_declines_to_validate() => _result.SequenceNumber.ShouldEqual(EventSequenceNumber.Unavailable.Value);
    [Fact] void should_not_put_the_before_first_value_on_the_wire() => _result.SequenceNumber.ShouldNotEqual(EventSequenceNumber.BeforeFirst.Value);
    [Fact] void should_still_declare_the_narrowing() => _result.EventSourceType.ShouldEqual("Customer");
}
