// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.System;
using Cratis.Chronicle.Patches;
using Cratis.Chronicle.Patching;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.Core.Specs.Patches.for_RepeatMoveSystemEventStoreDatabases;

/// <summary>
/// Patches are recorded by name, so sharing the name of the patch it repeats would have it skipped as already applied.
/// </summary>
public class when_applying_up : given.a_repeat_move_patch
{
    async Task Because() => await _patch.Up();

    [Fact] async Task should_move_the_system_event_store_to_its_configured_location() => await _storage.Received(1).MoveSystemEventStoreTo(SystemEventStoreLocation.Configured);
    [Fact] void should_run_after_the_release_it_ships_after() => (_patch.Version > new SemanticVersion(20, 0, 0)).ShouldBeTrue();

    [Fact] void should_not_share_the_name_of_the_patch_it_repeats() => ((ICanApplyPatch)_patch).Name.ShouldNotEqual(nameof(MoveSystemEventStoreDatabases));
}
