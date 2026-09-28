// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Sequences.for_NamedTagQueryCriterionConverters;

public class when_converting_a_wildcard_with_explicit_values : Specification
{
    Exception _exception;

    void Because() => _exception = Catch.Exception(() => new Contracts.Sequences.NamedTagQueryCriterion
    {
        Name = "account",
        AnyValue = true,
        Values = ["one"]
    }.ToApi());

    [Fact] void should_refuse_conflicting_criteria() => _exception.ShouldBeOfExactType<Storage.EventSequences.InvalidNamedTagCriterion>();
}
