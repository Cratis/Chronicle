// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Transactions;

namespace Cratis.Chronicle.Testing.ReadModels.for_DecisionReadsForTesting;

public class when_an_untyped_writer_changes_a_declared_stream : given.a_monthly_history
{
    DecisionRead<MonthlyWithdrawals> _read;
    IUnitOfWork _unit;

    async Task Establish()
    {
        _read = await ReadMonth();
        await _store.EventLog.Append(_source, new MoneyWithdrawn(50), "Transactions", "2026-10");
    }

    async Task Because() => _unit = await Commit(_read);

    [Fact] void should_not_guard_events_without_the_declared_source_type() => _unit.IsSuccess.ShouldBeTrue();
    [Fact] void should_not_fold_untyped_events() => _read.Instance!.Withdrawn.ShouldEqual(100m);
}
