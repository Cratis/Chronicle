// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.when_committing;

public class with_legacy_none_then_a_narrowed_guard : given.a_unit_of_work
{
    ConcurrencyScope _scope;

    void Establish()
    {
        _scope = new(42, "source", "stream");
        _unitOfWork.AddEvent(EventSequenceId.Log, "source", new object(), Causation.Unknown(), concurrencyScope: ConcurrencyScope.None);
        _unitOfWork.AddEvent(EventSequenceId.Log, "source", new object(), Causation.Unknown(), concurrencyScope: _scope);
    }

    Task Because() => _unitOfWork.Commit();

    [Fact] void should_send_only_the_real_guard() => _concurrencyScopesAppended.Values.ShouldContainOnly(_scope);
    [Fact] void should_keep_both_events() => _eventsAppended.Count().ShouldEqual(2);
}
