// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.EventSequences.Concurrency.for_ConcurrencyScopeConverters.when_converting_to_sequences_contract;

public class with_special_sequence_numbers : Specification
{
    ConcurrencyScope _scopeMax;
    ConcurrencyScope _scopeUnavailable;
    Contracts.Sequences.ConcurrencyScope _resultMax;
    Contracts.Sequences.ConcurrencyScope _resultUnavailable;

    void Establish()
    {
        _scopeMax = new ConcurrencyScope(EventSequenceNumber.Max);
        _scopeUnavailable = new ConcurrencyScope(EventSequenceNumber.Unavailable);
    }

    void Because()
    {
        _resultMax = _scopeMax.ToSequencesContract();
        _resultUnavailable = _scopeUnavailable.ToSequencesContract();
    }

    [Fact] void should_set_max_sequence_number_correctly() => _resultMax.SequenceNumber.ShouldEqual(EventSequenceNumber.Max.Value);
    [Fact] void should_set_unavailable_sequence_number_correctly() => _resultUnavailable.SequenceNumber.ShouldEqual(EventSequenceNumber.Unavailable.Value);
}
