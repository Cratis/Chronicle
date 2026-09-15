// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Patterns;
using Orleans.TestKit;

namespace Cratis.Chronicle.Patterns.for_PatternMiner.when_persisting;

/// <summary>
/// Persisting a scope rewrites everything that currently survives for it, so it happens on the interval rather
/// than per mined batch - the write cost is bounded by how many scopes acted, no matter how many events they
/// produced.
/// </summary>
public class and_the_interval_elapses : given.a_pattern_miner
{
    static readonly PatternGroupingKey _scope = "user-42";

    async Task Establish()
    {
        for (var count = 0; count < 20; count++)
        {
            await _miner.Mine([Features(_scope, "ApproveExpenseReport")]);
        }

        await _patterns.DidNotReceiveWithAnyArgs().Save(default!);
    }

    async Task Because() => await _silo.FireAllTimersAsync();

    [Fact] async Task should_save_the_surviving_patterns_once_for_the_touched_scope() =>
        await _patterns.Received(1).Save(Arg.Is<IEnumerable<BehaviorPattern>>(patterns => patterns.Any() && patterns.All(pattern => pattern.GroupingKey == _scope)));

    [Fact] async Task should_remove_everything_no_longer_surviving_for_the_touched_scope() =>
        await _patterns.Received(1).RemoveAllExcept(_scope, Arg.Any<IEnumerable<FacetSetKey>>());
}
