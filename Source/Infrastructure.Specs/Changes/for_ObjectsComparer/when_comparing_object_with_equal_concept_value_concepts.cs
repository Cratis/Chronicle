// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Changes.for_ObjectComparer;

public class when_comparing_object_with_equal_concept_value_concepts : given.an_object_comparer
{
    record TheConcept(string Value) : ConceptAs<string>(Value);
    record TheType(TheConcept Concept);

    TheType _left;
    TheType _right;

    bool _result;
    IEnumerable<PropertyDifference> _differences;

    void Establish()
    {
        _left = new(new("FortyTwo"));
        _right = new(new("FortyTwo"));
    }

    void Because() => _result = comparer.Compare(_left, _right, out _differences);

    [Fact] void should_be_equal() => _result.ShouldBeTrue();
    [Fact] void should_have_no_differences() => _differences.ShouldBeEmpty();
}
