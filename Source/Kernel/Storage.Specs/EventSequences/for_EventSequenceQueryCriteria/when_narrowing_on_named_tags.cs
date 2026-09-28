// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.EventSequences.for_EventSequenceQueryCriteria.given;

namespace Cratis.Chronicle.Storage.EventSequences.for_EventSequenceQueryCriteria;

public class when_narrowing_on_named_tags
{
    static readonly DateTimeOffset _from = new(2026, 8, 1, 0, 0, 0, TimeSpan.Zero);

    static EventContext With(params NamedTag[] tags) => an_event.With("source", "Event", ["plain"], _from) with { NamedTags = tags };

    [Fact] void should_not_narrow_for_null_collection() => new EventSequenceQueryCriteria { NamedTags = null }.Matches(With()).ShouldBeTrue();
    [Fact] void should_not_narrow_for_empty_collection() => new EventSequenceQueryCriteria { NamedTags = [] }.Matches(With()).ShouldBeTrue();
    [Fact] void should_report_when_it_narrows() => new EventSequenceQueryCriteria { NamedTags = [new NamedTagCriterion("name")] }.HasNamedTags.ShouldBeTrue();
    [Fact] void should_match_any_value_of_a_name() => new EventSequenceQueryCriteria { NamedTags = [new NamedTagCriterion("name")] }.Matches(With(new NamedTag("name", string.Empty))).ShouldBeTrue();
    [Fact] void should_not_match_a_missing_name() => new EventSequenceQueryCriteria { NamedTags = [new NamedTagCriterion("name")] }.Matches(With(new NamedTag("other", "v"))).ShouldBeFalse();
    [Fact] void should_match_a_single_exact_value() => new EventSequenceQueryCriteria { NamedTags = [new NamedTagCriterion("name", ["a:b/c"])] }.Matches(With(new NamedTag("name", "a:b/c"))).ShouldBeTrue();
    [Fact] void should_match_any_value_in_set() => new EventSequenceQueryCriteria { NamedTags = [new NamedTagCriterion("name", ["a", "b"])] }.Matches(With(new NamedTag("name", "b"))).ShouldBeTrue();
    [Fact] void should_not_cross_match_name_and_value_on_different_pairs() => new EventSequenceQueryCriteria { NamedTags = [new NamedTagCriterion("name", ["v"])] }.Matches(With(new NamedTag("name", "other"), new NamedTag("other", "v"))).ShouldBeFalse();
    [Fact] void should_or_across_named_criteria() => new EventSequenceQueryCriteria { NamedTags = [new NamedTagCriterion("missing"), new NamedTagCriterion("name", ["v"])] }.Matches(With(new NamedTag("name", "v"))).ShouldBeTrue();
    [Fact] void should_combine_with_plain_tags() => new EventSequenceQueryCriteria(Tags: ["missing"]) { NamedTags = [new NamedTagCriterion("name")] }.Matches(With(new NamedTag("name", "v"))).ShouldBeFalse();
    [Fact] void should_match_source_date_plain_and_named_dimensions_together() =>
        new EventSequenceQueryCriteria(EventSourceId: "source", Tags: ["plain"], OccurredFrom: _from, OccurredTo: _from.AddDays(1))
        {
            NamedTags = [new NamedTagCriterion("name", ["v"])]
        }.Matches(With(new NamedTag("name", "v"))).ShouldBeTrue();
    [Fact] void should_not_match_without_named_tag_when_plain_source_and_date_match() =>
        new EventSequenceQueryCriteria(EventSourceId: "source", Tags: ["plain"], OccurredFrom: _from, OccurredTo: _from.AddDays(1))
        {
            NamedTags = [new NamedTagCriterion("name", ["v"])]
        }.Matches(With()).ShouldBeFalse();
    [Fact] void should_reject_another_source_even_when_date_and_named_tag_match() =>
        new EventSequenceQueryCriteria(EventSourceId: "other", OccurredFrom: _from) { NamedTags = [new NamedTagCriterion("name")] }
            .Matches(With(new NamedTag("name", "v"))).ShouldBeFalse();
    [Fact] void should_combine_with_date() => new EventSequenceQueryCriteria(OccurredFrom: _from.AddDays(1)) { NamedTags = [new NamedTagCriterion("name")] }.Matches(With(new NamedTag("name", "v"))).ShouldBeFalse();
    [Fact] void should_use_ordinal_case_for_name_and_value() => new EventSequenceQueryCriteria { NamedTags = [new NamedTagCriterion("NAME", ["V"])] }.Matches(With(new NamedTag("name", "V"), new NamedTag("NAME", "v"))).ShouldBeFalse();
    [Fact] void should_not_match_turkish_dotless_i_to_ascii_i() =>
        new EventSequenceQueryCriteria { NamedTags = [new NamedTagCriterion("I", ["VALUE"])] }
            .Matches(With(new NamedTag("ı", "VALUE"))).ShouldBeFalse();

    [Fact] void should_reject_null_named_criterion() =>
        Catch.Exception(() => _ = new EventSequenceQueryCriteria { NamedTags = [null!] }).ShouldBeOfExactType<InvalidNamedTagCriterion>();

    [Fact] void should_snapshot_supplied_named_criteria()
    {
        var named = new List<NamedTagCriterion> { new("name") };
        var criteria = new EventSequenceQueryCriteria { NamedTags = named };
        named.Clear();
        criteria.Matches(With()).ShouldBeFalse();
    }
}
