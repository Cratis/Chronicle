// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Patterns;

namespace Cratis.Chronicle.Patterns.for_PatternMiner.when_mining;

/// <summary>
/// The same scope name in two event stores is two different people's behavior. One miner serves every store the
/// server holds, so counting them into one sketch would let each store's behavior contaminate what the other
/// store reports - and what the other store persists.
/// </summary>
public class the_same_scope_in_two_event_stores : given.a_pattern_miner
{
    EventStoreName _otherEventStore;
    IEnumerable<BehaviorPattern> _forFirstStore;
    IEnumerable<BehaviorPattern> _forOtherStore;

    void Establish() => _otherEventStore = "some-other-store";

    void Because()
    {
        for (var count = 0; count < 20; count++)
        {
            _miner.Observe(_eventStore, _namespace, Features("user-42", "ApproveExpenseReport"));
        }

        for (var count = 0; count < 20; count++)
        {
            _miner.Observe(_otherEventStore, _namespace, Features("user-42", "SubmitExpenseReport", DayOfWeek.Friday, TimeBucket.Evening));
        }

        _forFirstStore = _miner.GetSurvivingPatterns(_eventStore, _namespace, "user-42");
        _forOtherStore = _miner.GetSurvivingPatterns(_otherEventStore, _namespace, "user-42");
    }

    [Fact] void should_not_leak_the_other_store_behavior_into_the_first() =>
        _forFirstStore.All(_ => _.Facets.ValueOf(FacetName.CommandType) != new FacetValue("SubmitExpenseReport")).ShouldBeTrue();

    [Fact] void should_not_leak_the_first_store_behavior_into_the_other() =>
        _forOtherStore.All(_ => _.Facets.ValueOf(FacetName.CommandType) != new FacetValue("ApproveExpenseReport")).ShouldBeTrue();

    [Fact] void should_count_only_the_store_own_observations() =>
        _forFirstStore.Concat(_forOtherStore).All(_ => _.Occurrences.Value == 20L).ShouldBeTrue();

    [Fact] void should_hold_full_support_in_each_store_on_its_own() =>
        _forFirstStore.Concat(_forOtherStore).All(_ => _.Support.Value == 1d).ShouldBeTrue();
}
