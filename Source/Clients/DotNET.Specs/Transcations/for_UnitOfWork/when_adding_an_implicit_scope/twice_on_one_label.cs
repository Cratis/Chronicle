// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.when_adding_an_implicit_scope;

public class twice_on_one_label : given.a_unit_with_implicit_scopes
{
    void Establish()
    {
        Guard(7);
        Guard(3);
        Guard(9);
        Enroll();
    }
    Task Because() => Commit();
    [Fact] void should_keep_the_minimum_boundary() => Assert.Equivalent(new ConcurrencyScope(3, _sourceId).ToSequencesContract(), ScopeFor(_sourceId));
}
