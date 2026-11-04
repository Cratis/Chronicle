// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.Core.Specs.Patches.for_MoveSystemEventStoreDatabases;

public class when_applying_down : given.a_move_system_event_store_databases_patch
{
    async Task Because() => await _patch.Down();

    [Fact] async Task should_move_the_system_event_store_back_to_its_legacy_location() => await _storage.Received(1).MoveSystemEventStoreTo(SystemEventStoreLocation.Legacy);
}
