// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Patching;

namespace Orleans.Hosting.for_ChronicleServerStartupTask.when_executing;

public class and_the_patch_manager_never_relocates : given.a_startup_task
{
    Exception? _failure;

    void Establish() => _patchRunner.ApplyPatches(Arg.Any<CancellationToken>())
        .Returns(_ => Task.FromException(new PatchManagerCouldNotRelocate(new SemanticVersion(19, 100, 0))));

    async Task Because() => _failure = await Catch.Exception(Execute);

    [Fact] void should_log_the_release_mix() => LogMessages.Any(_ => _.Contains("unpublished: 0", StringComparison.Ordinal) && _.Contains(Cratis.Chronicle.Sys.KernelVersion.Current.ToString(), StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_request_the_release_summary() => _clusterReleases.Received(1).Summarize();
    [Fact] void should_fail_startup_with_the_failure_of_the_patch_runner() => _failure.ShouldBeOfExactType<PatchManagerCouldNotRelocate>();
    [Fact] async Task should_ask_the_runner_once() => await _patchRunner.Received(1).ApplyPatches(Arg.Any<CancellationToken>());
}
