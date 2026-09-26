// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Sequences.for_EventSequenceQueryCriteriaFactory.when_creating_with_invalid_named_tags;

public class and_json_contains_an_unknown_value_field : Specification
{
    Exception _exception;

    void Because() => _exception = Catch.Exception(() => EventSequenceQueryCriteriaFactory.CreateWithNamedTags(new(), null, "[{\"name\":\"account\",\"value\":\"one\"}]"));

    [Fact] void should_refuse_a_typo_instead_of_matching_any_value() => _exception.ShouldBeOfExactType<Storage.EventSequences.InvalidNamedTagCriterion>();
}
