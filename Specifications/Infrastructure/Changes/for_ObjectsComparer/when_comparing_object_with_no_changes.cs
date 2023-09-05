// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Changes.for_ObjectComparer;

public class when_comparing_object_with_no_changes : given.an_object_comparer
{
    record TheType(string StringValue, int IntValue);

    TheType left;
    TheType right;

    bool result;

    IEnumerable<PropertyDifference> differences;

    void Establish()
    {
        left = new TheType("FortyTwo", 42);
        right = new TheType("FortyTwo", 42);
    }

    void Because() => result = comparer.Equals(left, right, out differences);

    [Fact] void should_be_considered_equal() => result.ShouldBeTrue();
    [Fact] void should_not_have_any_differences() => differences.ShouldBeEmpty();
}
