// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.System;
using Cratis.Chronicle.Patching;

namespace Orleans.Hosting.for_ChronicleServerStartupTask.when_executing;

public class and_the_patch_manager_manifest_never_arrives : given.a_startup_task
{
    Exception? _failure;

    void Establish() => _patchManager.ApplyPatchesFor(Arg.Any<SemanticVersion>()).Returns(_ =>
        Task.FromException<PatchOutcome>(new OrleansException(ChronicleServerStartupTask.PatchManagerPlacementFailure + "2.")));

    async Task Because() => _failure = await Catch.Exception(Execute);

    [Fact] void should_fail_startup() => _failure.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] async Task should_stop_after_five_attempts() => await _patchManager.Received(5).ApplyPatchesFor(Arg.Any<SemanticVersion>());
    [Fact] void should_request_the_release_summary() => _clusterReleases.Received(1).Summarize();
    [Fact] void should_remove_the_caller_hint() => RequestContext.Get(PatchManagerRelocation.CallerSiloKey).ShouldBeNull();
}
