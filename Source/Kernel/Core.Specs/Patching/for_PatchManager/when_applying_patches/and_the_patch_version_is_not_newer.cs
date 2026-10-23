// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Patching.for_PatchManager.when_applying_patches;

public class and_the_patch_version_is_not_newer : given.a_patch_manager
{
    void Establish()
    {
        var patchVersion = _patch.Version;
        _system.GetVersion().Returns(patchVersion);
    }

    async Task Because()
    {
        await CreateManager();
        await _manager.ApplyPatches();
    }

    [Fact] async Task should_not_apply_the_patch() => await _patch.DidNotReceive().Up();
    [Fact] async Task should_record_the_kernel_version() => await _systemStorage.Received(1).SetPatchingKernelVersion(_version);
}
