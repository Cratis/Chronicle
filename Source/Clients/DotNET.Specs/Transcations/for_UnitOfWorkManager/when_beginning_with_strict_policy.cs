// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Transactions.for_UnitOfWorkManager;

public class when_beginning_with_strict_policy : Specification
{
    IUnitOfWork _unit;
    Exception _error;

    async Task Because()
    {
        var manager = new UnitOfWorkManager(Substitute.For<IEventStore>(), null, UnitOfWorkLifecyclePolicy.Strict);
        _unit = manager.Begin(CorrelationId.New());
        await _unit.Rollback();
        _error = Record.Exception(() => _unit.AddEvent(EventSequenceId.Log, "late", new object(), Causation.Unknown()));
    }

    [Fact] void should_pass_the_policy_to_the_new_unit() => _error.ShouldBeOfExactType<UnitOfWorkIsCompleted>();
}
