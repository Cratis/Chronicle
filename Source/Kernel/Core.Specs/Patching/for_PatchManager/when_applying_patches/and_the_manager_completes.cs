// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Patching.for_PatchManager.when_applying_patches;

public class and_the_manager_completes : given.a_patch_manager
{
    PatchOutcome _result;

    async Task Establish()
    {
        _worker.ApplyPatchesFor(Arg.Any<Cratis.Choreography.Patching.SemanticVersion>()).Returns(Cratis.Choreography.Patching.PatchOutcome.Completed);
        await CreateManager();
    }

    async Task Because() => _result = await _manager.ApplyPatchesFor(_caller);

    [Fact] void should_complete() => _result.ShouldEqual(PatchOutcome.Completed);
    [Fact] async Task should_hand_over_the_version_of_the_caller() => await _worker.Received(1).ApplyPatchesFor(new Cratis.Choreography.Patching.SemanticVersion(19, 100, 0, "rc.1", "abc"));
}
