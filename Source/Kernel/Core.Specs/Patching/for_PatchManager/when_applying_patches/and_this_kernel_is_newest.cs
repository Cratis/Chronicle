// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.System;

namespace Cratis.Chronicle.Patching.for_PatchManager.when_applying_patches;

public class and_this_kernel_is_newest : given.a_patch_manager
{
    void Establish() => _systemStorage.GetPatchingKernelVersion().Returns(new SemanticVersion(19, 99, 0));

    async Task Because()
    {
        await CreateManager();
        await _manager.ApplyPatches();
    }

    [Fact] async Task should_apply_the_patch() => await _patch.Received(1).Up();
    [Fact] async Task should_record_the_kernel_version() => await _systemStorage.Received(1).SetPatchingKernelVersion(_version);
    [Fact] async Task should_record_the_last_patch_version_separately() => await _system.Received(1).SetVersion(_patch.Version);
}
