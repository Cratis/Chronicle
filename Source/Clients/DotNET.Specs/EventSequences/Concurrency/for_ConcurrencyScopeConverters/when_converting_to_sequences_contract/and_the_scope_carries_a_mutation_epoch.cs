// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.Concurrency.for_ConcurrencyScopeConverters.when_converting_to_sequences_contract;

public class and_the_scope_carries_a_mutation_epoch : Specification
{
    Contracts.Sequences.ConcurrencyScope _result;
    Contracts.Sequences.ConcurrencyScope _withoutEpoch;

    void Because()
    {
        var scope = new ConcurrencyScope(42UL, "source") { MutationEpoch = 6UL };
        _result = scope.ToSequencesContract();
        _withoutEpoch = new ConcurrencyScope(42UL, "source").ToSequencesContract();
    }

    [Fact] void should_set_the_epoch() => _result.MutationEpoch.ShouldEqual(6UL);
    [Fact] void should_leave_it_unset_when_the_scope_has_none() => _withoutEpoch.MutationEpoch.ShouldBeNull();
}
