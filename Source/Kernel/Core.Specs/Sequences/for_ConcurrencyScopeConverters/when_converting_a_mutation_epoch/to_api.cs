// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;

namespace Cratis.Chronicle.Sequences.for_ConcurrencyScopeConverters.when_converting_a_mutation_epoch;

public class to_api : Specification
{
    ConcurrencyScope _result;

    void Because() => _result = new Concepts.EventSequences.Concurrency.ConcurrencyScope(4, true, null, null, null, null) { MutationEpoch = new EventSequenceMutationEpoch(6) }.ToApi();

    [Fact] void should_carry_the_epoch() => _result.MutationEpoch.ShouldEqual(6UL);
}
