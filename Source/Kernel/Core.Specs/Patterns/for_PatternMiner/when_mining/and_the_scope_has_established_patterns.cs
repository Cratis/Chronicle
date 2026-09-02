// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Patterns;

namespace Cratis.Chronicle.Patterns.for_PatternMiner.when_mining;

/// <summary>
/// The activation dies with its silo while the patterns it survived into do not. A scope acting for the first
/// time in an activation's life must continue from its established behavior - a fresh sketch would hold its first
/// events with full support, and the next flush would rewrite the scope from that, wiping what was established.
/// </summary>
public class and_the_scope_has_established_patterns : given.a_pattern_miner
{
    static readonly PatternGroupingKey _scope = "user-42";
    static readonly FacetSet _establishedFacets = new(
    [
        new Facet(FacetName.CommandType, "ApproveExpenseReport"),
        new Facet(FacetName.Day, "Monday"),
        new Facet(FacetName.TimeBucket, "Morning")
    ]);

    BehaviorPattern _established;
    IEnumerable<BehaviorPattern> _surviving;

    void Establish()
    {
        _established = new BehaviorPattern(_scope, _establishedFacets, 20, 1d, 1d, 20d, Occurred, Occurred);
        _patterns.GetForScope(_scope).Returns([_established]);
    }

    async Task Because()
    {
        await _miner.Mine([Features(_scope, "SubmitExpenseReport", DayOfWeek.Friday, TimeBucket.Evening)]);
        await _miner.Mine([Features(_scope, "SubmitExpenseReport", DayOfWeek.Friday, TimeBucket.Evening)]);
        _surviving = await _miner.GetSurvivingPatterns(_scope);
    }

    [Fact] void should_keep_the_established_behavior() => _surviving.Any(_ => _.Facets.Key == _establishedFacets.Key).ShouldBeTrue();
    [Fact] void should_keep_the_established_counts() => _surviving.Single(_ => _.Facets.Key == _establishedFacets.Key).Occurrences.Value.ShouldEqual(20L);
    [Fact] void should_keep_the_established_confidence() => _surviving.Single(_ => _.Facets.Key == _establishedFacets.Key).Confidence.Value.ShouldEqual(1d);
    [Fact] void should_not_let_the_stray_newcomers_survive() =>
        _surviving.Any(_ => _.Facets.ValueOf(FacetName.CommandType).Value == "SubmitExpenseReport" && _.Specificity == 3).ShouldBeFalse();
    [Fact] async Task should_only_read_the_established_patterns_once_per_activation() => await _patterns.Received(1).GetForScope(_scope);
}
