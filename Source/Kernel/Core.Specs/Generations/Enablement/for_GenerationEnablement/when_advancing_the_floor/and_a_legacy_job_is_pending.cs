// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Generations.Enablement;

namespace Cratis.Chronicle.Generations.Enablement.for_GenerationEnablement.when_advancing_the_floor;

public class and_a_legacy_job_is_pending : given.a_store_with_a_pending_legacy_job
{
    Task Because() => AdvanceThenInitialize();

    [Fact] void should_advance_the_floor_because_it_is_a_binary_barrier() => _advanced.Value.ShouldBeOfExactType<GenerationFloorAdvanced>();
    [Fact] void should_leave_the_pending_job_untouched() => _namespace.Jobs.ReceivedCalls().All(call => call.GetMethodInfo().Name == "GetJobs").ShouldBeTrue();
    [Fact] void should_refuse_enablement() => _result.ShouldBeOfExactType<GenerationInitializationBlocked>();
    [Fact] void should_name_the_unresolved_legacy_job() => ((GenerationInitializationBlocked)_result).Blockers.Single().Kind.ShouldEqual(GenerationInitializationBlockerKind.LegacyReplayJobUnresolved);
    [Fact] void should_not_adopt_or_bind_anything() => (_catalog.Calls + _catalog.Records.Count + _catalog.Epochs.Count).ShouldEqual(0);
    [Fact] async Task should_not_record_reconciliation_or_enablement() => (await _audit.GetReceipts()).Any(_ => _.Step is GenerationInitializationStep.LegacyWorkReconciled or GenerationInitializationStep.Enabled).ShouldBeFalse();
}
