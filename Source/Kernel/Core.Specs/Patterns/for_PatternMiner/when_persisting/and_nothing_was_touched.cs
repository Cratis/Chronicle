// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Orleans.TestKit;

namespace Cratis.Chronicle.Patterns.for_PatternMiner.when_persisting;

public class and_nothing_was_touched : given.a_pattern_miner
{
    async Task Because() => await _silo.FireAllTimersAsync();

    [Fact] void should_not_touch_storage() => _patterns.DidNotReceiveWithAnyArgs().Save(default!);
    [Fact] void should_not_remove_anything() => _patterns.DidNotReceiveWithAnyArgs().RemoveAllExcept(default!, default!);
}
