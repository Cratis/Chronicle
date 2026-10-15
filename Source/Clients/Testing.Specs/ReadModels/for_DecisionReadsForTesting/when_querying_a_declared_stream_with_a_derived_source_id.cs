// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels.for_DecisionReadsForTesting;

public class when_querying_a_declared_stream_with_a_derived_source_id : given.a_monthly_history
{
    public record AccountId(string Value) : EventSourceId(Value);

    MonthlyWithdrawals _result;

    async Task Because() => _result = await _store.ReadModels.GetInstanceById<MonthlyWithdrawals>(new AccountId(_source.Value), "2026-10");

    [Fact] void should_find_the_instance_folded_for_the_base_source() => _result.ShouldNotBeNull();
    [Fact] void should_fold_only_that_month() => _result.Withdrawn.ShouldEqual(100m);
}
