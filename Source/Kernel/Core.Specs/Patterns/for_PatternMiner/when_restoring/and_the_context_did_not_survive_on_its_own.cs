// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Patterns;

namespace Cratis.Chronicle.Patterns.for_PatternMiner.when_restoring;

/// <summary>
/// Only surviving patterns are persisted, and a pure context rarely survives on its own - its confidence is just
/// its support. A pattern restored without its context would re-derive at zero confidence and be swept away on
/// the first flush, so the restore must synthesize the context frequency back from the confidence that was
/// written with the pattern.
/// </summary>
public class and_the_context_did_not_survive_on_its_own : given.a_pattern_miner
{
    EventStoreName _restoredInto;
    BehaviorPattern[] _established;
    BehaviorPattern[] _restored;

    void Establish()
    {
        _restoredInto = "store-after-restart";

        // Six approvals in one slot against fourteen submissions elsewhere: the approving pattern survives with
        // full confidence, while its Monday-morning context alone holds three tenths support - below the halves
        // confidence a pure context needs - and is not persisted.
        for (var count = 0; count < 6; count++)
        {
            _miner.Observe(_eventStore, _namespace, Features("user-42", "ApproveExpenseReport"));
        }

        for (var count = 0; count < 14; count++)
        {
            _miner.Observe(_eventStore, _namespace, Features("user-42", "SubmitExpenseReport", DayOfWeek.Friday, TimeBucket.Evening));
        }

        _established = [.. _miner.GetSurvivingPatterns(_eventStore, _namespace, "user-42")];
    }

    void Because()
    {
        _miner.Restore(_restoredInto, _namespace, "user-42", _established);
        _restored = [.. _miner.GetSurvivingPatterns(_restoredInto, _namespace, "user-42")];
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
        _restored.Select(_ => _.Facets.Key).Order().ShouldContainOnly(_established.Select(_ => _.Facets.Key).Order());
}
