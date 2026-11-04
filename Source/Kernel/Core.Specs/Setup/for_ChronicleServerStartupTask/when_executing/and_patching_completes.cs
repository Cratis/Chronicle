// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Orleans.Hosting.for_ChronicleServerStartupTask.when_executing;

public class and_patching_completes : given.a_startup_task
{
    Task Because() => Execute();

    [Fact] async Task should_apply_the_patches_once() => await _patchRunner.Received(1).ApplyPatches(Arg.Any<CancellationToken>());
    [Fact] void should_not_ask_the_release_mix_to_be_described() => _clusterReleases.DidNotReceive().Summarize();
}
