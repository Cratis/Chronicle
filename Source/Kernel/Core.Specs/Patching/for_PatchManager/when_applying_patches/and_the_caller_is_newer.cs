// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.System;
using Moq;

namespace Cratis.Chronicle.Patching.for_PatchManager.when_applying_patches;

public class and_the_caller_is_newer : given.a_patch_manager
{
    PatchOutcome _result;

    async Task Because()
    {
        await CreateManager();
        _result = await _manager.ApplyPatchesFor(new SemanticVersion(19, 101, 0));
    }

    [Fact] void should_request_relocation() => _result.ShouldEqual(PatchOutcome.Relocate);
    [Fact] void should_deactivate() => _silo.GrainRuntime.Mock.Verify(runtime => runtime.DeactivateOnIdle(It.IsAny<IGrainContext>()), Times.Once);
    [Fact] async Task should_not_apply_patches() => await _patch.DidNotReceive().Up();
    [Fact] async Task should_not_record_a_kernel_version() => await _systemStorage.DidNotReceive().SetPatchingKernelVersion(Arg.Any<SemanticVersion>());
}
