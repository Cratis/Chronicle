// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Concepts;

namespace Aksio.Cratis.Changes.for_ObjectComparer;

public class when_comparing_object_with_equal_concept_value_concepts : given.an_object_comparer
{
    record TheConcept(string Value) : ConceptAs<string>(Value);
    record TheType(TheConcept Concept);

    TheType left;
    TheType right;

    bool result;
    IEnumerable<PropertyDifference> differences;

    void Establish()
    {
        left = new(new("FortyTwo"));
        right = new(new("FortyTwo"));
    }

    void Because() => result = comparer.Equals(left, right, out differences);

    [Fact] void should_be_equal() => result.ShouldBeTrue();
    [Fact] void should_have_no_differences() => differences.ShouldBeEmpty();
}
