// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.EventSequences.for_NamedTagCriterion;

public class when_creating_a_criterion
{
    [Fact] void should_reject_an_unset_name() => Catch.Exception(() => _ = new NamedTagCriterion(TagName.NotSet)).ShouldBeOfExactType<InvalidNamedTagCriterion>();
    [Fact] void should_reject_a_blank_name() => Catch.Exception(() => _ = new NamedTagCriterion("  ")).ShouldBeOfExactType<InvalidNamedTagCriterion>();
    [Fact] void should_reject_a_null_name() => Catch.Exception(() => _ = new NamedTagCriterion(null!)).ShouldBeOfExactType<InvalidNamedTagCriterion>();
    [Fact] void should_reject_an_empty_value_set() => Catch.Exception(() => _ = new NamedTagCriterion("n", [])).ShouldBeOfExactType<InvalidNamedTagCriterion>();
    [Fact] void should_reject_a_null_value_in_set() => Catch.Exception(() => _ = new NamedTagCriterion("n", [null!])).ShouldBeOfExactType<InvalidNamedTagCriterion>();
    [Fact] void should_accept_an_empty_string_value() => new NamedTagCriterion("n", [string.Empty]).Values!.Single().ShouldBeEmpty();
    [Fact] void should_treat_omitted_values_as_name_only() => new NamedTagCriterion("n").Values.ShouldBeNull();
    [Fact] void should_accept_null_values_on_copy() => (new NamedTagCriterion("n", ["v"]) with { Values = null }).Values.ShouldBeNull();
    [Fact] void should_reject_empty_values_on_copy() => Catch.Exception(() => _ = new NamedTagCriterion("n", ["v"]) with { Values = [] }).ShouldBeOfExactType<InvalidNamedTagCriterion>();
    [Fact] void should_reject_blank_names_on_copy() => Catch.Exception(() => _ = new NamedTagCriterion("n") with { Name = TagName.NotSet }).ShouldBeOfExactType<InvalidNamedTagCriterion>();
    [Fact] void should_snapshot_values() => ShouldSnapshotValues();

    static void ShouldSnapshotValues()
    {
        var values = new List<string> { "v" };
        var criterion = new NamedTagCriterion("n", values);
        values.Clear();
        criterion.Values!.Single().ShouldEqual("v");
    }
}
