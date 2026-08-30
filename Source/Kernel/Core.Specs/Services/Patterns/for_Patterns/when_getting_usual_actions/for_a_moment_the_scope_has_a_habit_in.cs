// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Patterns;
using Pattern = Cratis.Chronicle.Contracts.Patterns.Pattern;

namespace Cratis.Chronicle.Services.Patterns.for_Patterns.when_getting_usual_actions;

/// <summary>
/// The question the feature exists for, asked through the service: a day and a time of day in, a command out. The
/// same request answered by <see cref="Chronicle.Services.Patterns.Patterns.GetPatterns"/> comes back naming the
/// day and the time it was handed, because a pattern constraining a command is not a subset of a context that
/// names none.
/// </summary>
public class for_a_moment_the_scope_has_a_habit_in : given.a_patterns_service
{
    IEnumerable<Pattern> _answers;
    IEnumerable<Pattern> _descriptions;

    async Task Because()
    {
        _answers = await _service.GetUsualActions(Request());
        _descriptions = await _service.GetPatterns(Request());
    }

    [Fact] void should_answer_with_the_command() =>
        _answers.First().Facets[FacetName.CommandType.Value].ShouldEqual("RegisterInvoice");

    [Fact] void should_answer_with_how_likely_it_is() => _answers.First().Confidence.ShouldEqual(0.95d);
    [Fact] void should_answer_only_with_actions() => _answers.Count().ShouldEqual(1);

    [Fact] void should_not_answer_with_an_action_from_another_day() =>
        _answers.Any(pattern => pattern.Facets[FacetName.CommandType.Value] == "MatchInvoice").ShouldBeFalse();

    [Fact] void should_leave_the_describing_query_naming_no_command() =>
        _descriptions.Any(pattern => pattern.Facets.ContainsKey(FacetName.CommandType.Value)).ShouldBeFalse();

    static Contracts.Patterns.GetPatternsRequest Request() => new()
    {
        EventStore = EventStore,
        Namespace = EventStoreNamespaceName.Default,
        GroupingKey = Scope,
        Context = new Dictionary<string, string>
        {
            { FacetName.Day.Value, "Monday" },
            { FacetName.TimeBucket.Value, "Morning" }
        }
    };
}
