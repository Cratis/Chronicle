// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Patching;

namespace Orleans.Hosting.for_ChronicleServerStartupTask.when_executing;

public class and_the_patch_manager_relocates_to_a_newer_kernel : given.a_startup_task
{
    Exception? _failure;

    void Establish() => _patchManager.ApplyPatchesFor(Arg.Any<Cratis.Chronicle.Concepts.System.SemanticVersion>())
        .Returns(Task.FromResult(PatchOutcome.Relocate), Task.FromResult(PatchOutcome.Completed));

    async Task Because() => _failure = await Catch.Exception(Execute);

    [Fact] void should_not_fail() => _failure.ShouldBeNull();
    [Fact] async Task should_ask_again_after_the_relocation() => await _patchManager.Received(2).ApplyPatchesFor(Arg.Any<Cratis.Chronicle.Concepts.System.SemanticVersion>());
}
