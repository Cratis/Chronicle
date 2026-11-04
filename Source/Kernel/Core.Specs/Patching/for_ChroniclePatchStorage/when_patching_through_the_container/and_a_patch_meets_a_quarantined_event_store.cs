// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Patching.for_ChroniclePatchStorage.when_patching_through_the_container;

public class and_a_patch_meets_a_quarantined_event_store : given.a_patching_run_through_the_container
{
    Exception _error;

    async Task Because()
    {
        _error = await Catch.Exception(ApplyPatches);
    }

    [Fact] void should_complete_startup() => _error.ShouldBeNull();
    [Fact] void should_run_the_patch_once() => _storage.Received(1).GetEventStores();
    [Fact] void should_not_open_the_quarantined_event_store() => _storage.DidNotReceive().GetEventStore(Held);
    [Fact] void should_not_record_the_patch_as_applied() => _systemStorage.Patches.DidNotReceive().Save(Arg.Any<Concepts.Patching.Patch>());
    [Fact] void should_not_move_the_system_version_past_the_patch() => _system.DidNotReceive().SetVersion(Arg.Any<Concepts.System.SemanticVersion>());
}
