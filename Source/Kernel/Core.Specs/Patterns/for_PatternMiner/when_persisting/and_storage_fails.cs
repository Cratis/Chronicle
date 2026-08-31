// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Patterns;
using Orleans.TestKit;

namespace Cratis.Chronicle.Patterns.for_PatternMiner.when_persisting;

public class and_storage_fails : given.a_pattern_miner
{
    static readonly PatternGroupingKey _scope = "user-42";

    async Task Establish()
    {
        _patterns.Save(Arg.Any<IEnumerable<BehaviorPattern>>()).Returns(
            _ => throw new Exception("storage broke"),
            _ => Task.CompletedTask);

        await _miner.Mine([Features(_scope, "ApproveExpenseReport")]);

        // The first tick fails against storage; the scope must stay marked so the next tick tries again.
        await _silo.FireAllTimersAsync();
    }

    async Task Because() => await _silo.FireAllTimersAsync();

    [Fact] async Task should_retry_the_scope_on_the_next_tick() => await _patterns.Received(2).Save(Arg.Any<IEnumerable<BehaviorPattern>>());
    [Fact] async Task should_remove_what_no_longer_survives_once_saving_succeeds() => await _patterns.Received(1).RemoveAllExcept(_scope, Arg.Any<IEnumerable<FacetSetKey>>());
}
