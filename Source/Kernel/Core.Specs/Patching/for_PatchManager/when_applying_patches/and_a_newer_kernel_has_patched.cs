// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.System;

namespace Cratis.Chronicle.Patching.for_PatchManager.when_applying_patches;

public class and_a_newer_kernel_has_patched : given.a_patch_manager
{
    void Establish() => _systemStorage.GetPatchingKernelVersion().Returns(new SemanticVersion(19, 101, 0));

    async Task Because()
    {
        await CreateManager();
        await _manager.ApplyPatches();
    }

    [Fact] async Task should_not_apply_the_patch() => await _patch.DidNotReceive().Up();
    [Fact] async Task should_not_change_the_mark() => await _systemStorage.DidNotReceive().SetPatchingKernelVersion(Arg.Any<SemanticVersion>());
    [Fact] async Task should_not_read_the_patch_version() => await _system.DidNotReceive().GetVersion();
}
