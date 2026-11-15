// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Patching.for_PatchManager.when_applying_patches;

public class and_the_manager_relocates : given.a_patch_manager
{
    PatchOutcome _result;

    async Task Establish()
    {
        _worker.ApplyPatchesFor(Arg.Any<Cratis.Choreography.Patching.SemanticVersion>()).Returns(Cratis.Choreography.Patching.PatchOutcome.Relocate);
        await CreateManager();
    }

    async Task Because() => _result = await _manager.ApplyPatchesFor(_caller);

    [Fact] void should_ask_the_caller_to_come_back() => _result.ShouldEqual(PatchOutcome.Relocate);
}
