// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.Core.Specs.Patches.for_MoveSystemEventStoreDatabases;

public class when_applying_up : given.a_move_system_event_store_databases_patch
{
    async Task Because() => await _patch.Up();

    [Fact] async Task should_move_the_system_event_store_to_its_configured_location() => await _storage.Received(1).MoveSystemEventStoreTo(SystemEventStoreLocation.Configured);
}
