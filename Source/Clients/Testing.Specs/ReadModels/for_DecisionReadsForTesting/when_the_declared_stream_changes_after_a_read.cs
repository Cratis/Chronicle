// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Transactions;

namespace Cratis.Chronicle.Testing.ReadModels.for_DecisionReadsForTesting;

public class when_the_declared_stream_changes_after_a_read : given.a_monthly_history
{
    DecisionRead<MonthlyWithdrawals> _read;
    IUnitOfWork _unit;

    async Task Establish()
    {
        _read = await ReadMonth();
        await _store.EventLog.Append(_source, new MoneyWithdrawn(50), "Transactions", "2026-10", "BankAccount");
    }

    async Task Because() => _unit = await Commit(_read);

    [Fact] void should_refuse_the_commit() => _unit.IsSuccess.ShouldBeFalse();
    [Fact] void should_identify_the_source_conflict() => _unit.GetDecisionConflicts().ShouldContain(conflict => conflict.Key.Value == _source.Value);
}
