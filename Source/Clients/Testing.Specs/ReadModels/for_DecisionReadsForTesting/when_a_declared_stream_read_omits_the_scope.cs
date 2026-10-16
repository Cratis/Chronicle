// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.Testing.ReadModels.for_DecisionReadsForTesting;

public class when_a_declared_stream_read_omits_the_scope : given.a_monthly_history
{
    [Fact] async Task should_refuse_detached_key_reads() => ((DecisionReadRefused)await Catch.Exception(() => _store.GetDecisionReads().GetDetached<MonthlyWithdrawals>(_source))).Reason.ShouldEqual(DecisionReadRefusalReason.ReadModelRequiresStreamScope);
    [Fact] async Task should_refuse_plain_key_reads() => ((DecisionReadRefused)await Catch.Exception(() => _store.ReadModels.GetInstanceById<MonthlyWithdrawals>((ReadModelKey)_source))).Reason.ShouldEqual(DecisionReadRefusalReason.ReadModelRequiresStreamScope);
    [Fact] async Task should_refuse_collection_reads() => ((DecisionReadRefused)await Catch.Exception(() => _store.ReadModels.GetInstances<MonthlyWithdrawals>())).Reason.ShouldEqual(DecisionReadRefusalReason.ReadModelRequiresStreamScope);
    [Fact] void should_refuse_watch_reads() => ((DecisionReadRefused)Catch.Exception(() => _store.ReadModels.Watch<MonthlyWithdrawals>())).Reason.ShouldEqual(DecisionReadRefusalReason.ReadModelRequiresStreamScope);
    [Fact] async Task should_refuse_explicitly_enrolled_key_reads()
    {
        using var unit = _store.UnitOfWorkManager.Begin(CorrelationId.New());
        var error = await Catch.Exception(() => _store.GetDecisionReads().GetFor<MonthlyWithdrawals>(unit, _source));
        ((DecisionReadRefused)error).Reason.ShouldEqual(DecisionReadRefusalReason.ReadModelRequiresStreamScope);
    }
    [Fact] async Task should_refuse_ambient_key_reads()
    {
        using var unit = _store.UnitOfWorkManager.Begin(CorrelationId.New());
        var error = await Catch.Exception(() => _store.GetDecisionReads().Get<MonthlyWithdrawals>(_source));
        ((DecisionReadRefused)error).Reason.ShouldEqual(DecisionReadRefusalReason.ReadModelRequiresStreamScope);
    }
}
