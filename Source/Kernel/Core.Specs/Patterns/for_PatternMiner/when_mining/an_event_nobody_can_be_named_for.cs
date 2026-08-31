// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Patterns;

namespace Cratis.Chronicle.Patterns.for_PatternMiner.when_mining;

/// <summary>
/// Behavior belongs to somebody. An event with no scope could only be counted into a catch-all that every
/// unattributed append in the store pours into, which is noise rather than a pattern.
/// </summary>
public class an_event_nobody_can_be_named_for : given.a_pattern_miner
{
    IEnumerable<BehaviorPattern> _result;

    void Because()
    {
        for (var count = 0; count < 20; count++)
        {
            _miner.Observe(_eventStore, _namespace, Features(PatternGroupingKey.Unspecified, "ApproveExpenseReport"));
        }

        _result = _miner.GetSurvivingPatterns(_eventStore, _namespace, PatternGroupingKey.Unspecified);
    }

    [Fact] void should_not_mine_anything() => _result.ShouldBeEmpty();
}
