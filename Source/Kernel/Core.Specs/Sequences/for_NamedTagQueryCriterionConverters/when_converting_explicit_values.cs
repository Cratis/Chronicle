// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Sequences.for_NamedTagQueryCriterionConverters;

public class when_converting_explicit_values : Specification
{
    NamedTagQueryCriterion _criterion;

    void Because() => _criterion = new Contracts.Sequences.NamedTagQueryCriterion
    {
        Name = "account",
        Values = ["one", "two"]
    }.ToApi();

    [Fact] void should_preserve_the_exact_values() => _criterion.Values.ShouldContain("two");
    [Fact] void should_not_match_every_value() => _criterion.AnyValue.ShouldBeFalse();
}
