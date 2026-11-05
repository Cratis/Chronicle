// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Core.Specs.Patches.for_RepeatMoveSystemEventStoreDatabases;

public class when_applying_down : given.a_repeat_move_patch
{
    async Task Because() => await _patch.Down();

    [Fact] void should_not_touch_storage() => _storage.ReceivedCalls().ShouldBeEmpty();
}
