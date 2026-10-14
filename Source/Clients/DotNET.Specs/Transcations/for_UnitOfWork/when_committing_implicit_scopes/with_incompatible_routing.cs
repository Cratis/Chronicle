// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.when_committing_implicit_scopes;

public class with_incompatible_routing : given.a_unit_with_implicit_scopes
{
    void Establish()
    {
        Guard();
        _enrollment.AddEvents(EventSequenceId.Log, [Item("one"), Item("two")], []);
    }
    Task Because() => Commit();
    [Fact] void should_preserve_the_routing_incompatibility() => _exception.ShouldBeOfExactType<IncompatibleConcurrencyScopesForEventSource>();
}
