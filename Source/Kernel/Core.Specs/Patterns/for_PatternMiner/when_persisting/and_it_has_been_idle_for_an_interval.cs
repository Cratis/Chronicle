// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Patterns;

namespace Cratis.Chronicle.Patterns.for_PatternMiner.when_persisting;

/// <summary>
/// A pass that finds nothing mined for a whole interval has nothing to wait for: the miner leaves the supervisor so that
/// the activation can be collected, and what it held is in storage already.
/// </summary>
public class and_it_has_been_idle_for_an_interval : given.a_pattern_miner
{
    static readonly PatternGroupingKey _scope = "user-42";

    async Task Establish()
    {
        await _miner.Mine([Features(_scope, "ApproveExpenseReport")]);
        await RunSupervisorCheck();
        _supervisor.ClearReceivedCalls();
        _clock.Advance(TimeSpan.FromSeconds(6));
    }

    async Task Because() => await RunSupervisorCheck();

    [Fact] void should_leave_the_supervisor() => _supervisor.ReceivedWithAnyArgs(1).Unregister(default!);
}
