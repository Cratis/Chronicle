// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;

namespace Cratis.Chronicle.Sequences.for_ConcurrencyScopeConverters.when_converting_a_mutation_epoch;

public class to_chronicle : Specification
{
    Concepts.EventSequences.Concurrency.ConcurrencyScope _result;

    void Because() => _result = new Contracts.Sequences.ConcurrencyScope { SequenceNumber = 4, EventSourceId = true, MutationEpoch = 6 }.ToApi().ToChronicle();

    [Fact] void should_carry_the_epoch() => _result.MutationEpoch.ShouldEqual(new EventSequenceMutationEpoch(6));
}
