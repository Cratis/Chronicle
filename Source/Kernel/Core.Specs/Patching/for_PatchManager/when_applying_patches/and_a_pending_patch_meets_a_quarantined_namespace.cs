// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.System;

namespace Cratis.Chronicle.Patching.for_PatchManager.when_applying_patches;

public class and_a_pending_patch_meets_a_quarantined_namespace : given.a_patch_manager_with_a_quarantined_namespace
{
    Exception _error;

    async Task Because()
    {
        await CreateManager();
        _error = await Catch.Exception(_manager.ApplyPatches);
    }

    [Fact] void should_complete_startup() => _error.ShouldBeNull();
    [Fact] void should_reach_the_namespace_that_is_not_quarantined() => _eventStoreStorage.Received().GetNamespace(Open);
    [Fact] void should_not_reach_the_quarantined_namespace() => _eventStoreStorage.DidNotReceive().GetNamespace(Held);
    [Fact] void should_not_record_the_patch_as_applied() => _systemStorage.Patches.DidNotReceive().Save(Arg.Any<Concepts.Patching.Patch>());
    [Fact] void should_not_move_the_system_version_past_the_patch() => _system.DidNotReceive().SetVersion(Arg.Any<SemanticVersion>());
}
