// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.Testing.ReadModels.for_DecisionReadsForTesting;

public class when_a_model_declares_its_stream : given.a_monthly_history
{
    DecisionRead<MonthlyWithdrawals> _read;

    async Task Because() => _read = await ReadMonth();

    [Fact] void should_fold_only_that_month_and_source_type() => _read.Instance!.Withdrawn.ShouldEqual(100m);
    [Fact] void should_keep_the_owner_as_the_model_key() => _read.Instance!.Id.ShouldEqual(_source.Value);
    [Fact] async Task should_answer_a_plain_stream_query_with_the_same_instance() => (await _store.ReadModels.GetInstanceById<MonthlyWithdrawals>(_source, "2026-10")).ShouldEqual(_read.Instance);
    [Fact] async Task should_answer_another_month_independently() => (await _store.ReadModels.GetInstanceById<MonthlyWithdrawals>(_source, "2026-11")).Withdrawn.ShouldEqual(200m);
    [Fact] async Task should_return_null_for_a_month_without_events() => (await _store.ReadModels.GetInstanceById<MonthlyWithdrawals>(_source, "2026-12")).ShouldBeNull();
}
