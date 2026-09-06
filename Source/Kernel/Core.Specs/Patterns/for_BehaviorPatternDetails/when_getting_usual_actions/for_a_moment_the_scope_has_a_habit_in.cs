// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Patterns;

namespace Cratis.Chronicle.Patterns.for_BehaviorPatternDetails.when_getting_usual_actions;

/// <summary>
/// The question the feature exists for: a day and a time of day in, a command out. The same context answered by
/// <see cref="BehaviorPatternDetails.MatchingPatterns"/> comes back naming the day and the time it was handed,
/// because a pattern constraining a command is not a subset of a context that names none.
/// </summary>
public class for_a_moment_the_scope_has_a_habit_in : given.mined_patterns
{
    IEnumerable<BehaviorPatternDetails> _answers;
    IEnumerable<BehaviorPatternDetails> _descriptions;

    async Task Because()
    {
        _answers = await BehaviorPatternDetails.UsualActions(
            EventStore, EventStoreNamespaceName.Default, Scope, Context(), _storage, _vocabulary, _matcher, _options);

        _descriptions = await BehaviorPatternDetails.MatchingPatterns(
            EventStore, EventStoreNamespaceName.Default, Scope, Context(), _storage, _vocabulary, _generator, _matcher, _options);
    }

    [Fact] void should_answer_with_the_command() =>
        _answers.First().Facets[FacetName.CommandType.Value].ShouldEqual("RegisterInvoice");

    [Fact] void should_answer_with_how_likely_it_is() => _answers.First().Confidence.ShouldEqual(0.95d);
    [Fact] void should_answer_only_with_actions() => _answers.Count().ShouldEqual(1);

    [Fact] void should_not_answer_with_an_action_from_another_day() =>
        _answers.Any(pattern => pattern.Facets[FacetName.CommandType.Value] == "MatchInvoice").ShouldBeFalse();

    [Fact] void should_leave_the_describing_query_naming_no_command() =>
        _descriptions.Any(pattern => pattern.Facets.ContainsKey(FacetName.CommandType.Value)).ShouldBeFalse();

    static Dictionary<string, string> Context() => new()
    {
        { FacetName.Day.Value, "Monday" },
        { FacetName.TimeBucket.Value, "Morning" }
    };
}
