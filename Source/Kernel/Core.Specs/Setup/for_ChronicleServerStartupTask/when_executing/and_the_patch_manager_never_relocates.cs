// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Patching;

namespace Orleans.Hosting.for_ChronicleServerStartupTask.when_executing;

public class and_the_patch_manager_never_relocates : given.a_startup_task
{
    Exception? _failure;

    void Establish() => _patchManager.ApplyPatchesFor(Arg.Any<Cratis.Chronicle.Concepts.System.SemanticVersion>())
        .Returns(Task.FromResult(PatchOutcome.Relocate));

    async Task Because() => _failure = await Catch.Exception(Execute);

    [Fact] void should_fail_startup() => _failure.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] async Task should_stop_after_the_bounded_attempts() => await _patchManager.Received(5).ApplyPatchesFor(Arg.Any<Cratis.Chronicle.Concepts.System.SemanticVersion>());
}
