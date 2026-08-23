// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.EventSequences.Concurrency.for_ConcurrencyScopeConverters.when_converting_to_contract;

/// <summary>
/// The other side of the same rule. Rewriting the number field is confined to the before-first expectation - an
/// ordinary scope sends the number it was given and leaves the new field clear, so an older kernel validates it
/// exactly as it always did.
/// </summary>
public class and_the_scope_expects_an_actual_sequence_number : Specification
{
    ConcurrencyScope _scope;
    Contracts.EventSequences.Concurrency.ConcurrencyScope _result;

    void Establish() => _scope = new ConcurrencyScope(
        new EventSequenceNumber(42),
        new EventSourceId("some-event-source-id"));

    void Because() => _result = _scope.ToContract();

    [Fact] void should_send_the_expected_sequence_number() => _result.SequenceNumber.ShouldEqual(42UL);
    [Fact] void should_not_declare_the_expectation() => _result.ExpectsNoMatchingEvent.ShouldBeFalse();
}
