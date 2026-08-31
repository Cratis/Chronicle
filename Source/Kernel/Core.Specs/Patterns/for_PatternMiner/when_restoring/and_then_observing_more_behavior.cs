// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Patterns;

namespace Cratis.Chronicle.Patterns.for_PatternMiner.when_restoring;

/// <summary>
/// Without restoring, the first event after a restart would be a fresh sketch's only observation - held with full
/// support, and rewriting the scope from it would wipe the established behavior in favor of whatever happened to
/// occur first. Restored counts must keep their standing against a stray newcomer.
/// </summary>
public class and_then_observing_more_behavior : given.a_pattern_miner
{
    EventStoreName _restoredInto;
    IEnumerable<BehaviorPattern> _surviving;

    void Establish()
    {
        _restoredInto = "store-after-restart";

        for (var count = 0; count < 20; count++)
        {
            _miner.Observe(_eventStore, _namespace, Features("user-42", "ApproveExpenseReport"));
        }

        _miner.Restore(_restoredInto, _namespace, "user-42", _miner.GetSurvivingPatterns(_eventStore, _namespace, "user-42"));
    }

    void Because()
    {
        _miner.Observe(_restoredInto, _namespace, Features("user-42", "SubmitExpenseReport", DayOfWeek.Friday, TimeBucket.Evening));
        _surviving = _miner.GetSurvivingPatterns(_restoredInto, _namespace, "user-42");
    }

    [Fact] void should_keep_the_established_behavior() =>
        _surviving.Any(_ =>
            _.Facets.ValueOf(FacetName.CommandType).Value == "ApproveExpenseReport" &&
            _.Facets.ValueOf(FacetName.Day).Value == "Monday" &&
            _.Facets.ValueOf(FacetName.TimeBucket).Value == "Morning").ShouldBeTrue();

    [Fact] void should_keep_the_established_counts() =>
        _surviving.Where(_ => _.Facets.ValueOf(FacetName.CommandType).Value == "ApproveExpenseReport").All(_ => _.Occurrences.Value == 20L).ShouldBeTrue();

    [Fact] void should_not_let_a_single_new_event_survive() =>
        _surviving.Any(_ => _.Facets.ValueOf(FacetName.CommandType).Value == "SubmitExpenseReport").ShouldBeFalse();
}
