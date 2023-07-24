// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;

namespace Aksio.Cratis.Changes.for_ObjectComparer;

public class when_comparing_expando_objects_and_left_is_null_and_right_is_empty : given.an_object_comparer
{
    dynamic left;
    dynamic right;

    bool result;

    IEnumerable<PropertyDifference> differences;

    void Establish()
    {
        left = null;
        right = new ExpandoObject();
    }

    void Because() => result = comparer.Equals(left, right, out differences);

    [Fact] void should_be_considered_equal() => result.ShouldBeTrue();
    [Fact] void should_have_no_differences() => differences.ShouldBeEmpty();
}
