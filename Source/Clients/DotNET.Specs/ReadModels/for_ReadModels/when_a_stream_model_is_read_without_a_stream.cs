// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ReadModels.for_ReadModels;

public class when_a_stream_model_is_read_without_a_stream : given.a_stream_model
{
    [Fact] async Task should_refuse_generic_instance_reads() => ((DecisionReadRefused)await Catch.Exception(() => _readModels.GetInstanceById<MonthlyState>("account"))).Reason.ShouldEqual(DecisionReadRefusalReason.ReadModelRequiresStreamScope);
    [Fact] async Task should_refuse_runtime_type_instance_reads() => ((DecisionReadRefused)await Catch.Exception(() => _readModels.GetInstanceById(typeof(MonthlyState), "account"))).Reason.ShouldEqual(DecisionReadRefusalReason.ReadModelRequiresStreamScope);
    [Fact] async Task should_refuse_collection_reads() => ((DecisionReadRefused)await Catch.Exception(() => _readModels.GetInstances<MonthlyState>())).Reason.ShouldEqual(DecisionReadRefusalReason.ReadModelRequiresStreamScope);
    [Fact] void should_refuse_watching() => ((DecisionReadRefused)Catch.Exception(() => _readModels.Watch<MonthlyState>())).Reason.ShouldEqual(DecisionReadRefusalReason.ReadModelRequiresStreamScope);
    [Fact] void should_refuse_observation() => ((DecisionReadRefused)Catch.Exception(() => _readModels.Observe<MonthlyState>())).Reason.ShouldEqual(DecisionReadRefusalReason.ReadModelRequiresStreamScope);
    [Fact] void should_refuse_the_underlying_watcher() => ((DecisionReadRefused)Catch.Exception(() => _readModels.GetWatcherFor<MonthlyState>())).Reason.ShouldEqual(DecisionReadRefusalReason.ReadModelRequiresStreamScope);
}
