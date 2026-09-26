// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Sequences.for_EventSequenceQueryCriteriaFactory.when_creating_with_invalid_named_tags;

public class and_json_values_are_empty : Specification
{
    Exception _exception;

    void Because() => _exception = Catch.Exception(() => EventSequenceQueryCriteriaFactory.CreateWithNamedTags(new(), null, "[{\"name\":\"account\",\"values\":[]}]"));

    [Fact] void should_refuse_an_empty_value_set() => _exception.ShouldBeOfExactType<Storage.EventSequences.InvalidNamedTagCriterion>();
}
