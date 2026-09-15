// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Patterns;

namespace Cratis.Chronicle.Patterns.for_PatternMiner.when_deactivating;

public class with_touched_scopes : given.a_pattern_miner
{
    static readonly PatternGroupingKey _scope = "user-42";

    async Task Establish() => await _miner.Mine([Features(_scope, "ApproveExpenseReport")]);

    async Task Because() => await _silo.DeactivateAsync(_miner);

    [Fact] async Task should_persist_what_the_interval_had_not_flushed_yet() => await _patterns.Received(1).Save(Arg.Any<IEnumerable<BehaviorPattern>>());
}
