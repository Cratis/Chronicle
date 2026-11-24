// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Patterns;

namespace Cratis.Chronicle.Patterns.for_PatternMiner.when_mining;

/// <summary>
/// Persisting is the silo supervisor's to schedule, so mining asks it for a pass once - however many batches follow -
/// and no grain timer exists per namespace.
/// </summary>
public class and_it_is_mined_in_several_batches : given.a_pattern_miner
{
    static readonly PatternGroupingKey _scope = "user-42";

    async Task Because()
    {
        await _miner.Mine([Features(_scope, "ApproveExpenseReport")]);
        await _miner.Mine([Features(_scope, "ApproveExpenseReport")]);
        await _miner.Mine([Features(_scope, "ApproveExpenseReport")]);
    }

    [Fact] void should_register_with_the_supervisor_once() => _supervisor.ReceivedWithAnyArgs(1).Register(default!, default, default, default);
    [Fact] void should_ask_for_a_pass_every_persistence_interval() => _checkInterval.ShouldEqual(TimeSpan.FromSeconds(5));
    [Fact] void should_not_register_a_grain_timer() => _silo.TimerRegistry.NumberOfActiveTimers.ShouldEqual(0);
}
