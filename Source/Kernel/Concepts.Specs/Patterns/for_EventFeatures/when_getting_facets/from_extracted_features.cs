// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Patterns.for_EventFeatures.when_getting_facets;

public class from_extracted_features : Specification
{
    EventFeatures _features;
    IReadOnlyDictionary<FacetName, FacetValue> _result;

    void Establish() => _features = new EventFeatures(
        "user-42",
        "ApproveExpenseReport",
        InitiatorType.Agent,
        "agent-7",
        "user-42",
        "SubmitExpenseReport",
        "correlation-1",
        "ExpenseReport",
        2026,
        8,
        DayOfWeek.Monday,
        TimeBucket.Morning,
        new DateTimeOffset(2026, 8, 24, 9, 15, 0, TimeSpan.Zero));

    void Because() => _result = _features.AsFacets();

    [Fact] void should_expose_the_command_type() => _result[FacetName.CommandType].Value.ShouldEqual("ApproveExpenseReport");
    [Fact] void should_expose_the_initiator_type_by_name() => _result[FacetName.InitiatorType].Value.ShouldEqual("Agent");
    [Fact] void should_expose_the_command_a_level_up() => _result[FacetName.CausedByCommand].Value.ShouldEqual("SubmitExpenseReport");
    [Fact] void should_expose_the_aggregate_type() => _result[FacetName.AggregateType].Value.ShouldEqual("ExpenseReport");
    [Fact] void should_expose_the_day_by_name() => _result[FacetName.Day].Value.ShouldEqual("Monday");
    [Fact] void should_expose_the_time_bucket_by_name() => _result[FacetName.TimeBucket].Value.ShouldEqual("Morning");
    [Fact] void should_expose_the_year() => _result[FacetName.Year].Value.ShouldEqual("2026");
    [Fact] void should_expose_the_month() => _result[FacetName.Month].Value.ShouldEqual("8");
    [Fact] void should_not_expose_the_grouping_key_as_a_facet() => _result.ContainsKey("GroupingKey").ShouldBeFalse();
}
