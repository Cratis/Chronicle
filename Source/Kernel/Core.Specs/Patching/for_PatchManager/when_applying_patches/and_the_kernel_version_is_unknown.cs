// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.System;

namespace Cratis.Chronicle.Patching.for_PatchManager.when_applying_patches;

public class and_the_kernel_version_is_unknown : given.a_patch_manager
{
    void Establish()
    {
        _version = new SemanticVersion(1, 0, 0);
        _systemStorage.GetPatchingKernelVersion().Returns(new SemanticVersion(19, 101, 0));
    }

    async Task Because()
    {
        await CreateManager();
        await _manager.ApplyPatches();
    }

    [Fact] async Task should_apply_the_patch() => await _patch.Received(1).Up();
    [Fact] async Task should_not_record_a_kernel_version() => await _systemStorage.DidNotReceive().SetPatchingKernelVersion(Arg.Any<SemanticVersion>());
}
