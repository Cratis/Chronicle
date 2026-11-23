// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Patterns;

namespace Cratis.Chronicle.Patterns.for_PatternMiner.when_persisting;

/// <summary>
/// A pass right after a flush finds nothing to write, but the miner was busy a moment ago, so it stays registered.
/// </summary>
public class and_it_was_mined_a_moment_ago : given.a_pattern_miner
{
    static readonly PatternGroupingKey _scope = "user-42";

    async Task Establish()
    {
        await _miner.Mine([Features(_scope, "ApproveExpenseReport")]);
        await RunSupervisorCheck();
        _supervisor.ClearReceivedCalls();
        _clock.Advance(TimeSpan.FromSeconds(1));
    }

    async Task Because() => await RunSupervisorCheck();

    [Fact] void should_stay_with_the_supervisor() => _supervisor.DidNotReceiveWithAnyArgs().Unregister(default!);
}
