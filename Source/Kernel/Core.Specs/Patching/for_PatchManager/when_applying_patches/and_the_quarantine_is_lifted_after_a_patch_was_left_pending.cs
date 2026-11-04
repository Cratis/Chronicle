// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.System;
using Cratis.Chronicle.Storage.Naming;

namespace Cratis.Chronicle.Patching.for_PatchManager.when_applying_patches;

public class and_the_quarantine_is_lifted_after_a_patch_was_left_pending : given.a_patch_manager_with_a_quarantined_event_store
{
    async Task Because()
    {
        await CreateManager();
        await _manager.ApplyPatches();

        _resourceNames.Publish(new ResourceNameAuditResult([], [], []));
        _quarantineLifted = true;
        await _manager.ApplyPatches();
    }

    [Fact] void should_reach_the_event_store_that_was_quarantined() => _storage.Received(1).GetEventStore(Held);
    [Fact] void should_record_the_patch_as_applied_once() => _systemStorage.Patches.Received(1).Save(Arg.Is<Concepts.Patching.Patch>(_ => _.Name == "RemoveUnhashedUniqueConstraintValues"));
    [Fact] void should_move_the_system_version_to_the_patch() => _system.Received(1).SetVersion(new SemanticVersion(19, 112, 0));
}
