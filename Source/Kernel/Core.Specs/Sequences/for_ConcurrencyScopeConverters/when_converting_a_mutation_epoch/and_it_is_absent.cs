// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Sequences.for_ConcurrencyScopeConverters.when_converting_a_mutation_epoch;

public class and_it_is_absent : Specification
{
    Concepts.EventSequences.Concurrency.ConcurrencyScope _result;

    void Because() => _result = new Contracts.Sequences.ConcurrencyScope { SequenceNumber = 4, EventSourceId = true }.ToApi().ToChronicle();

    [Fact] void should_not_invent_an_epoch() => _result.MutationEpoch.ShouldBeNull();
}
