// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.EventSequences.Concurrency.for_ConcurrencyScopeConverters.when_converting_to_contract;

public class with_special_sequence_numbers : Specification
{
    ConcurrencyScope _scopeMax;
    ConcurrencyScope _scopeUnavailable;
    Contracts.EventSequences.Concurrency.ConcurrencyScope _resultMax;
    Contracts.EventSequences.Concurrency.ConcurrencyScope _resultUnavailable;

    void Establish()
    {
        _scopeMax = new ConcurrencyScope(EventSequenceNumber.Max);
        _scopeUnavailable = new ConcurrencyScope(EventSequenceNumber.Unavailable);
    }

    void Because()
    {
        _resultMax = _scopeMax.ToContract();
        _resultUnavailable = _scopeUnavailable.ToContract();
    }

    [Fact] void should_set_max_sequence_number_correctly() => _resultMax.SequenceNumber.ShouldEqual(EventSequenceNumber.Max.Value);
    [Fact] void should_set_unavailable_sequence_number_correctly() => _resultUnavailable.SequenceNumber.ShouldEqual(EventSequenceNumber.Unavailable.Value);
}
