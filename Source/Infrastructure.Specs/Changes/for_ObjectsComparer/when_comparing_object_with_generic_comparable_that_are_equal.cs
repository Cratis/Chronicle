// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Changes.for_ObjectComparer;

public class when_comparing_object_with_generic_comparable_that_are_equal : given.an_object_comparer
{
    record TheType(MyGenericComparable Comparable);

    TheType left;
    TheType right;
    bool result;
    IEnumerable<PropertyDifference> differences;

    void Establish()
    {
        left = new(new(0));
        right = new(new(0));
    }

    void Because() => result = comparer.Compare(left, right, out differences);

    [Fact] void should_be_equal() => result.ShouldBeTrue();
    [Fact] void should_have_no_differences() => differences.ShouldBeEmpty();
}
