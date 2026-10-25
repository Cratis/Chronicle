// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Generations;

namespace Cratis.Chronicle.Projections.Generations.for_ProjectionGenerationBuild;

/// <summary>Admission reserves a private candidate while the first generation continues serving.</summary>
public class when_starting_a_build : given.a_generation_build_owner
{
    /// <summary>Starts the candidate operation.</summary>
    async Task Because() => _result = await Start();

    /// <summary>The operation is admitted.</summary>
    [Fact] void should_admit_the_operation() => _result.Result.ShouldEqual(GenerationBuildAdmissionResult.Admitted);

    /// <summary>The new generation is the next monotonic number.</summary>
    [Fact] void should_reserve_the_second_generation() => _result.Record!.Identity.Generation.ShouldEqual(new MaterializationGeneration(2));

    /// <summary>The owner does not dispatch or advance lifecycle state.</summary>
    [Fact] void should_leave_the_candidate_building() => _result.Record!.State.ShouldEqual(MaterializationGenerationState.Building);

    /// <summary>The destination stays private.</summary>
    [Fact] void should_use_a_private_destination() => _result.Record!.Identity.Destination.ShouldNotEqual(_first.Identity.Destination);

    /// <summary>The first fixed grant belongs to the caller's operation.</summary>
    [Fact] void should_admit_the_requested_attempt() => _result.Grant!.Attempt.ShouldEqual(_request.Operation);

    /// <summary>First admission advances epoch zero exactly once.</summary>
    [Fact] void should_issue_the_first_grant_epoch() => _result.Grant!.Epoch.ShouldEqual(1L);

    /// <summary>The grant is attached to the immutable requested revision.</summary>
    [Fact] void should_bind_the_execution_revision() => _result.Grant!.Binding.Revision.ShouldEqual(_request.Execution.Revision);

    /// <summary>The returned authority is durably admitted by the real grant store.</summary>
    [Fact] async Task should_persist_the_fixed_grant() => (await _catalog.GetAdmitted(Store, _result.Grant!.Binding)).ShouldEqual(_result.Grant);

    /// <summary>The reservation persists the execution inputs.</summary>
    [Fact] async Task should_persist_the_execution() => (await _builds.GetExecution(Store, _key, _result.Record!.Identity.Generation)).ShouldEqual(_request.Execution);

    /// <summary>The first generation remains the serving identity.</summary>
    [Fact] async Task should_preserve_the_active_generation() => (await Binding()).Active.ShouldEqual(MaterializationGeneration.First);

    /// <summary>The serving destination is not published to the candidate.</summary>
    [Fact] async Task should_preserve_the_serving_destination() => (await Binding()).Destination.ShouldEqual(_first.Identity.Destination);

    /// <summary>The serving epoch is not advanced or revoked.</summary>
    [Fact] async Task should_preserve_the_serving_epoch() => (await Binding()).Epoch.ShouldEqual(ServingEpoch.Initial);

    /// <summary>The serving generation record is unchanged.</summary>
    [Fact] async Task should_preserve_the_serving_record() => (await Binding()).Generations.Single(_ => _.Identity.Generation.IsFirst).ShouldEqual(_first);
}
