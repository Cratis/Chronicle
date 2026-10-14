// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.when_adding_an_implicit_scope;

public class for_the_same_label_on_different_sequences : given.a_unit_with_implicit_scopes
{
    void Establish()
    {
        Guard(2, sequence: "other");
        Guard(7);
        Guard(1, sequence: "other");
        Guard(5);
        Enroll();
    }
    Task Because() => Commit();
    [Fact] void should_take_the_minimum_only_within_the_bound_sequence() => Assert.Equivalent(new ConcurrencyScope(5, _sourceId).ToSequencesContract(), ScopeFor(_sourceId));
}
