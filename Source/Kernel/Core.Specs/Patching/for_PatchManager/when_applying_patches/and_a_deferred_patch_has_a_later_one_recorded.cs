// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.System;

namespace Cratis.Chronicle.Patching.for_PatchManager.when_applying_patches;

public class and_a_deferred_patch_has_a_later_one_recorded : given.a_deferred_patch_and_a_later_recorded_one
{
    async Task Because()
    {
        await CreateManager();
        await _manager.ApplyPatches();

        _deferred.Skips = false;
        await _manager.ApplyPatches();
    }

    [Fact] void should_record_the_later_patch_once() => _systemStorage.Patches.Received(1).Save(Arg.Is<Concepts.Patching.Patch>(_ => _.Name == "later-patch"));
    [Fact] void should_record_the_deferred_patch_once_it_is_reached() => _systemStorage.Patches.Received(1).Save(Arg.Is<Concepts.Patching.Patch>(_ => _.Name == "deferred-patch"));
    [Fact] void should_not_move_the_version_while_the_patch_was_deferred() => _system.Received(1).SetVersion(Arg.Any<SemanticVersion>());
    [Fact] void should_move_the_version_past_both_patches_once_the_deferred_patch_completes() => _system.Received(1).SetVersion(LaterVersion);
}
