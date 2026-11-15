// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Patching.for_PatchManager.when_applying_patches;

public class and_asked_without_a_caller : given.a_patch_manager
{
    async Task Establish()
    {
        _worker.ApplyPatchesFor(Arg.Any<Cratis.Choreography.Patching.SemanticVersion>()).Returns(Cratis.Choreography.Patching.PatchOutcome.Completed);
        await CreateManager();
    }

    Task Because() => _manager.ApplyPatches();

    [Fact] async Task should_patch_as_this_kernel() => await _worker.Received(1).ApplyPatchesFor(Chronicle.Sys.KernelVersion.Current.ToPatching());
}
