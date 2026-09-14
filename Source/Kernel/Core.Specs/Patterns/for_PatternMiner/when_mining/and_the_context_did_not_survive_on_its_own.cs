// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Patterns;
using Cratis.Chronicle.Storage.Patterns;

namespace Cratis.Chronicle.Patterns.for_PatternMiner.when_mining;

/// <summary>
/// Only surviving patterns are persisted, and a pure context rarely survives on its own - its confidence is just
/// its support. A pattern restored without its context would re-derive at zero confidence and be swept away on
/// the first flush, so the restore must synthesize the context frequency back from the confidence that was
/// written with the pattern.
/// </summary>
public class and_the_context_did_not_survive_on_its_own : given.a_pattern_miner
{
    static readonly PatternGroupingKey _scope = "user-42";

    BehaviorPattern[] _established;
    BehaviorPattern[] _restored;

    async Task Establish()
    {
        // Six approvals in one slot against fourteen submissions elsewhere: the approving pattern survives with
        // full confidence, while its Monday-morning context alone holds three tenths support - below the halves
        // confidence a pure context needs - and is not persisted.
        for (var count = 0; count < 6; count++)
        {
            await _miner.Mine([Features(_scope, "ApproveExpenseReport")]);
        }

        for (var count = 0; count < 14; count++)
        {
            await _miner.Mine([Features(_scope, "SubmitExpenseReport", DayOfWeek.Friday, TimeBucket.Evening)]);
        }

        _established = [.. await _miner.GetSurvivingPatterns(_scope)];
    }

    async Task Because()
    {
        // A second activation against a store already holding the established patterns - the life after a
        // restart. It mines one stray event, which is what forces the scope to be restored first.
        var patterns = Substitute.For<IBehaviorPatternStorage>();
        patterns.GetForScope(_scope).Returns(_established);
        var (_, restartedMiner) = await CreateMiner(_eventStore, _namespace, patterns);

        await restartedMiner.Mine([Features(_scope, "RejectExpenseReport", DayOfWeek.Wednesday, TimeBucket.Afternoon)]);
        _restored = [.. await restartedMiner.GetSurvivingPatterns(_scope)];
    }

    static BehaviorPattern ApprovingAmong(IEnumerable<BehaviorPattern> patterns) => patterns.Single(_ =>
        _.Facets.ValueOf(FacetName.CommandType).Value == "ApproveExpenseReport" &&
        _.Facets.ValueOf(FacetName.Day).Value == "Monday" &&
        _.Facets.ValueOf(FacetName.TimeBucket).Value == "Morning" &&
        _.Specificity == 3);

    [Fact] void should_not_have_persisted_the_context_on_its_own() =>
        _established.Any(_ => _.Facets.Key == ApprovingAmong(_established).Facets.WithoutActions().Key).ShouldBeFalse();

    [Fact] void should_keep_the_established_pattern() => ApprovingAmong(_restored).ShouldNotBeNull();
    [Fact] void should_re_derive_it_with_its_confidence() => ApprovingAmong(_restored).Confidence.ShouldEqual(ApprovingAmong(_established).Confidence);
    [Fact] void should_re_derive_it_with_its_counts() => ApprovingAmong(_restored).Occurrences.Value.ShouldEqual(6L);
    [Fact] void should_hold_every_established_pattern() =>
        _restored.Select(_ => _.Facets.Key).Order().ShouldContain(_established.Select(_ => _.Facets.Key).Order().ToArray());
}
