// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Changes.for_ObjectComparer;

public class when_comparing_object_with_equal_guid_values : given.an_object_comparer
{
    record TheType(Guid TheGuid);

    TheType left;
    TheType right;

    bool result;
    IEnumerable<PropertyDifference> differences;

    void Establish()
    {
        left = new(Guid.Parse("240409fd-43db-4675-9345-025526c9c7e4"));
        right = new(Guid.Parse("240409fd-43db-4675-9345-025526c9c7e4"));
    }

    void Because() => result = comparer.Equals(left, right, out differences);

    [Fact] void should_be_equal() => result.ShouldBeTrue();
    [Fact] void should_have_no_differences() => differences.ShouldBeEmpty();
}
