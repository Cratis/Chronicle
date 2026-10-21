// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Transactions;

namespace Cratis.Chronicle.Testing.ReadModels.for_DecisionReadsForTesting;

public class when_another_month_changes_after_a_declared_stream_read : given.a_monthly_history
{
    DecisionRead<MonthlyWithdrawals> _read;
    IUnitOfWork _unit;

    async Task Establish()
    {
        _read = await ReadMonth();
        await _store.EventLog.Append(_source, new MoneyWithdrawn(50), "Transactions", "2026-11", "BankAccount");
    }

    async Task Because() => _unit = await Commit(_read);

    [Fact] void should_allow_the_commit() => _unit.IsSuccess.ShouldBeTrue();
    [Fact] void should_keep_the_october_fold() => _read.Instance!.Withdrawn.ShouldEqual(100m);
}
