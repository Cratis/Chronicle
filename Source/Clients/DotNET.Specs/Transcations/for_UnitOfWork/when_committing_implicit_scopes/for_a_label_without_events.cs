// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.when_committing_implicit_scopes;

public class for_a_label_without_events : given.a_unit_with_implicit_scopes
{
    void Establish()
    {
        Guard();
        Enroll(source: "other");
    }
    Task Because() => Commit();
    [Fact] void should_commit() => _exception.ShouldBeNull();
    [Fact] void should_send_the_independent_guard() => Assert.Equivalent(new ConcurrencyScope(5, _sourceId).ToSequencesContract(), ScopeFor(_sourceId));
}
